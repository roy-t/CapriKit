# Stubs

C# source generators target .NET Standard 2.0 and thus miss a lot of of the new C# features. However, a lot of these features rely on classes that the compiler or IDE recognize by name and are not true dependencies. The stubs in this folder allow us to still use modern features like records and `NotNullWhen`.
