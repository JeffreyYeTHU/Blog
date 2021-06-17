# 数据库迁移工具golang-migrate的使用

数据库迁移是软件迭代过程中非常重要的一部分。常见的方式有两种：
1. 使用平台ORM工具。例如在.NET中使用EF Core，在Java中使用Hibernate.
2. 使用平台独立的工具，如本文要介绍的golang-migrate。

使用平台ORM工具，有两个不足：其一，数据库迁移功能与代码深度耦合。它们是separate concern，它们的生命周期不同，它们的迭代速度与原因也通常不同，分而治之为佳。其二，迁移依赖于平台，使得数据从某种程度上也依赖于平台。

[golang-migrate](https://github.com/golang-migrate/migrate)是用go实现的一个数据库迁移工具。它具有支持CLI使用方式、平台无关、支持数据库、数据库迁移可纳入版本管理系统等优点，非常适合用于管理数据库迁移。

## Docker方式使用

Docker在手，天下我有。这个工具有docker image，因此可以非常方便的使用。

**step 1**

把migration文件放在合适的位置。例如，可以在项目中建一个migration文件夹，把所有文件放入其中。文件遵循的命名方式为`VersionNumber_MigrationName_Up/Down.sql`.例如：

* 001_create_user_table.up.sql
* 001_create_user_table.down.sql
* 002_add_email_to_user.up.sql
* 002_add_email_to_user.down.sql

小TIP，版本号使用数字，并加上前导的0，这样文件在系统中可以很好的按版本排序；而在数据库中可以很容易分别数据库所处版本。

**step 2**

基于创建的迁移源（source）进行迁移。这里以mysql为例。

```bash
 docker run -v D:\Jeffery\Github\golang-migrate-tutorial\migrator\migrations:/migrations --network host migrate/migrate --path=/migrations/ --database "mysql://zuru:123456@tcp(192.168.56.1:3306)/testdb" up 1
```

命令的使用注意事项：
1. 参数-v是用于告诉docker，需要寻找的迁移文件（source）在何处。
2. image的名称叫`migrate/migrate`
3. mysql的数据库连接串，要用双引号引起来。否则会报`ParserError：MissingPropertyName`
4. 连接串中的host地址，不能使用localhost或者127.0.0.1，使用当前主机IP设置中所配置的IP地址。否则会报连接错误`dial tcp 127.0.0.1:3306: connect: connection refused`
5. 命令最后的`up/down x`代表的是，以数据库当前的版本为起点，向前迁移x个版本/向后回滚x个版本。

数据库迁移后，数据库会新增一个名为`schema_migrations`的表格，用于记录版本信息。这个表格只包含两列。version列表征当前版本号，dirty列表征迁移是否成功。如果dirty为true则迁移失败。