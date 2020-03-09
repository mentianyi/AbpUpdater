## Abp updater

This is a tool to update [Abp](https://abp.io) [MicroService solution](https://github.com/abpframework/abp/tree/dev/samples/MicroserviceDemo) to your project.

Today it is only support one command : update.

**Update** command has four options :
- **-r** update projects file (.csproj) from projectreference to packagereference
- **-p** path of main .sln directory,if not set ,it will be current directory
-  **-d** the option convert from SqlServer to your EfProvider (eg.MySQL)
-  **-c** your connectionstrings.Multi-connections use ',' seperate.

### Usage:
for example
```
abpupdater update -p C:\Projects\abp\samples\MicroserviceDemo -r 2.2.0 -c Default:Server=(localdb)\\MSSQLLocalDB;Database=MsDemo_Identity;Trusted_Connection=True;MultipleActiveResultSets=true -d MySQL
```

This sample update the C:\Projects\abp\samples\MicroserviceDemo\*.sln ,it replace project file reference to package version 2.2.0,replace default connectionstring and efprovider to mysql.