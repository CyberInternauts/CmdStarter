using com.cyberinternauts.csharp.CmdStarter.Lib.Extensions;
using com.cyberinternauts.csharp.CmdStarter.Lib.Reflection;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Loader.DepencendyInjection;
using com.cyberinternauts.csharp.CmdStarter.Tests.Commands.Options;
using System.Collections;
using System.CommandLine;
using System.ComponentModel.Composition.Hosting;

namespace com.cyberinternauts.csharp.CmdStarter.Tests
{
    public static class TestsCommon
    {
        public const int NUMBER_OF_COMMANDS_IN_DEMO = 3; // Using a hardcoded value otherwise the test would be about the same code as the tested one.
        public const string ANY_CHAR_SYMBOL = CmdStarter.Lib.Starter.ANY_CHAR_SYMBOL;
        public const string ANY_CHAR_SYMBOL_INCLUDE_DOTS = CmdStarter.Lib.Starter.ANY_CHAR_SYMBOL_INCLUDE_DOTS;
        public const string MULTI_ANY_CHAR_SYMBOL = CmdStarter.Lib.Starter.MULTI_ANY_CHAR_SYMBOL;
        public const string MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS = CmdStarter.Lib.Starter.MULTI_ANY_CHAR_SYMBOL_INCLUDE_DOTS;
        public const string EXCLUSION_SYMBOL = CmdStarter.Lib.Starter.EXCLUSION_SYMBOL;
        public readonly static string ERRONEOUS_NAMESPACE = typeof(Commands.Erroneous.Boggus).Namespace ?? string.Empty;

        public readonly static IList<Type> CLASS_FILTERING_TYPES = new List<Type>
                {
                    typeof(Commands.Loader.Filtering.Starter),
                    typeof(Commands.Loader.Filtering.StarterA),
                    typeof(Commands.Loader.Filtering.StarterB),
                    typeof(Commands.Loader.Filtering.StarterOn),
                    typeof(Commands.Loader.Filtering.StarterOff),
                    typeof(Commands.Loader.Filtering.IO.Starter),
                    typeof(Commands.Loader.Filtering.A.IO.Starter),
                    typeof(Commands.Loader.Filtering.B.IO.Starter),
                    typeof(Commands.Loader.Filtering.AB.IO.Starter),
                    typeof(Commands.Loader.Filtering.NorthS.Starter),
                    typeof(Commands.Loader.Filtering.EastNS.Starter),
                    typeof(Commands.Loader.Filtering.NSouth.Starter)
                };

        public static void GlobalSetup()
        {
            _ = new DirectoryCatalog("."); // This is needed to ensure Tests.Commands assembly is loaded even if no class referenced
        }

        public static CmdStarter.Lib.Starter CreateCmdStarter()
        {
            Starter.SetDefaultFactory();

            var starter = new CmdStarter.Lib.Starter(); // Reset object to a new one, not to interfer between tests
            SetDefaultNamespaces(starter);
            return starter;
        }

        public static void SetDefaultNamespaces(CmdStarter.Lib.Starter starter)
        {
            starter.Namespaces = starter.Namespaces.Clear();
            if (TestsCommon.ERRONEOUS_NAMESPACE != string.Empty) // Remove, by default, erroneous namespace
            {
                starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + TestsCommon.ERRONEOUS_NAMESPACE);
            }
            // Remove, by default, dependency injection namespace
            starter.Namespaces = starter.Namespaces.Add(TestsCommon.EXCLUSION_SYMBOL + typeof(Dependent1).Namespace!);
        }

        public static void SetDefaultClasses(CmdStarter.Lib.Starter starter)
        {
            starter.Classes = starter.Classes.Clear();
        }

        public static void AssertCommandsAreEmpty(CmdStarter.Lib.Starter starter, bool includeTypes = true)
        {
            Assert.Multiple(() =>
            {
                if (includeTypes)
                {
                    Assert.That(starter.CommandsTypes, Is.Empty);
                }
                Assert.That(starter.CommandsTypesTree.Children, Is.Empty);
                Assert.That(starter.RootCommand.Subcommands, Is.Empty);
            });
        }

        public static void AssertCommandsExists(CmdStarter.Lib.Starter starter, bool onlyTypes = false)
        {
            Assert.Multiple(() =>
            {
                Assert.That(starter.CommandsTypes, Is.Not.Empty);
                if (onlyTypes) return;

                Assert.That(starter.CommandsTypesTree.Children, Is.Not.Empty);
                Assert.That(starter.RootCommand.Subcommands, Is.Not.Empty);
            });
        }

        public static void AssertIEnumerablesHaveSameElements<T>(IEnumerable<T> actual, IEnumerable<T> expected)
        {
            const string EMPTY_STRING = "empty";

            var joinedExpected = expected.Any() ? string.Join(",", expected) : EMPTY_STRING;
            var joinedActual = actual.Any() ? string.Join(",", actual) : EMPTY_STRING;
            var errorMessage = $"Expected: <{joinedExpected}> but was <{joinedActual}>";

            Assert.That(actual.Count(), Is.EqualTo(expected.Count()), errorMessage);
            Assert.That(actual.Except(expected), Is.Empty, errorMessage);
        }


        public static void AssertThrowsException<ExceptionType>(Action codeThatThrows, Action<ExceptionType> testOnException) where ExceptionType : Exception
        {
            string failingMessage = "Expected to throw " + typeof(ExceptionType).FullName;
            try
            {
                codeThatThrows();
                Assert.Fail(failingMessage);
            }
            catch (ExceptionType ex)
            {
                testOnException(ex);
            }
            catch (Exception ex)
            {
                if (ex is not ExceptionType)
                {
                    Assert.Fail(failingMessage);
                }
            }
        }

        public static List<string> PrepareOption(string optionName, object expectedValue)
        {
            var args = new List<string>();

            switch (Type.GetTypeCode(expectedValue.GetType()))
            {
                case TypeCode.Boolean:
                    args.Add(OptHandling.OPTION_PREFIX + optionName);
                    break;

                default:
                    if (expectedValue is not string && expectedValue is IEnumerable values)
                    {
                        foreach (var curValue in values)
                        {
                            var value = curValue.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(value)) continue;

                            args.Add(OptHandling.OPTION_PREFIX + optionName);
                            args.Add(value);
                        }
                    }
                    else
                    {
                        var value = expectedValue.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            args.Add(OptHandling.OPTION_PREFIX + optionName);
                            args.Add(value);
                        }
                    }
                    break;
            }

            return args;
        }

        public static string PrintOption(string optionName, object expectedValue)
        {
            return PrepareOption(optionName, expectedValue).Aggregate(
                (current, next) => 
                    current +
                    (string.IsNullOrEmpty(current) ? string.Empty : " ") +
                    (next.Contains(' ') ? "\"" : string.Empty) +
                    next.Replace("\"", "\\\"") +
                    (next.Contains(' ') ? "\"" : string.Empty)
                );
        }

        /// <summary>
        /// This method use reflexion and thus shall not be used to test reflexion of a method
        /// </summary>
        /// <param name="commandToGetProperties"></param>
        /// <param name="commandToGetOptions"></param>
        public static void AssertOptionsPresence(Command commandToGetProperties, Command commandToGetOptions)
        {
            var properties = Helper.GetProperties(commandToGetProperties);
            if (properties == null || !properties.Any())
            {
                Assert.That(commandToGetOptions.Options, Has.Count.EqualTo(0));
                return;
            }

            Assert.That(commandToGetOptions.Options, Has.Count.EqualTo(properties.Count()));
            var index = 0;
            foreach (var property in properties)
            {
                var option = commandToGetOptions.Options[index];
                Assert.That(option.Name, Is.EqualTo(property.Name.PascalToKebabCase()));
                index++;
            }
        }

        public static void AssertArguments(IStarterCommand commandToGetHandler, Command commandToGetArguments)
        {
            var parameters = commandToGetHandler.HandlingMethod.Method.GetParameters();
            Assert.That(parameters, Is.Not.Null);
            Assert.That(commandToGetArguments.Arguments, Has.Count.EqualTo(parameters.Length));

            Assert.Multiple(() =>
            {
                var index = 0;
                foreach (var parameter in parameters)
                {
                    var description = (parameter.GetCustomAttributes(false)
                        .FirstOrDefault(a => a is System.ComponentModel.DescriptionAttribute) as System.ComponentModel.DescriptionAttribute)
                        ?.Description;

                    var message = () => "Error for parameter:" + parameter.Name;
                    var arg = commandToGetArguments.Arguments[index];
                    Assert.That(arg.Name, Is.EqualTo(parameter.Name), message);
                    if (description != null)
                    {
                        Assert.That(arg.Description, Is.EqualTo(description), message);
                    }
                    else
                    {
                        Assert.That(String.IsNullOrEmpty(arg.Description), Is.True, message);
                    }
                    if (parameter.DefaultValue is not System.DBNull)
                    {
                        Assert.That(arg.HasDefaultValue, Is.True, message);
                        Assert.That(arg.GetDefaultValue(), Is.EqualTo(parameter.DefaultValue), message);
                    }
                    else
                    {
                        Assert.That(arg.HasDefaultValue, Is.False, message);
                    }

                    index++;
                }
            });
        }
    }
}
