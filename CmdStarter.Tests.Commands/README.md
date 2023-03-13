Rules for commands creation/usage
=================================

- Shall not add new commands in root (Only the Main command shall be there)
- Shall use a new namespace for a new group of commands to test
- The Erroneous namespace shall contains subnamespace each containing a configuration of commands that is wrong.
- The Erroneous namespace shall be automatically removed (Starter.Namespaces = "~NAMESPACE") (In [Setup] method of the class).
- When testing under Erroneous, shall use only one subnamespace at a time.