# Explaining AuthP's Roles

An AuthP's Role represent a set of features on your application that a user (human or machine) can access. Roles are normally named after the user's job, say "Sales Person", "Sales Manager", and so on. These Roles manage what pages/WebAPIs a logged in user can access.

## ASP.NET Roles - good, but some implementation limitations

The idea of using Roles started in ASP.NET MVC and is in ASP.NET Core. In ASP.NET Roles are hard-coded into your application via the `[Authorize(Roles = "Sales Person,Sales Manager")]`. The downsides of the ASP.NET Roles approach are:

- If you want to change what a Role can access you need to edit your application and redeploy it.
- In larger applications the authorize attributes get pretty long (e.g. `[Authorize(Roles = “Staff, SalesManager , DevManage, Admin, SuperAdmin”)]`) and hard to manage.

## AuthP's Roles - improving the implementation of the Roles concept

The AuthP's library keeps the Roles concept for users, but provides a lower-level concept called [Permissions](!!!!) that manage what pages/WebAPIs can be accessed. The mapping from a user to Permissions is held in a database so that you can use AuthP's admin features:

- Change what Permissions (i.e. what pages/WebAPI can be accessed) are in a AuthP's Role.
- Change want AuthP's Roles a user has.

The end result is you have a cleaner implementation of the Roles concept, and a more manageable pages/WebAPI scheme.

## Additional resources

- [Explaining AuthP's Permissions](!!!!)
- [Explaining AuthP's AuthUser](!!!!)
- [How AuthP's add Permissions to a logged-in user](!!!!)
- [Using Permissions to control access](!!!!)
