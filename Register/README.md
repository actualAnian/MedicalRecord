# a project showcasing example flow through an asp.net minimal api and graphql endpoint

example calls are in:


### setting up a database
the database is a tsql database

### configuration
copy `appsettings.example.json` to `appsettings.json` and fill in the connection string,
or provide it through the `ConnectionStrings__DefaultConnection` environment variable.

### connect to the database via terminal
docker exec -it medical-register-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U <your_username> -P <your_password> -C

### view tables
use MedicalRegister;
go

select column_name from information_schema.columns where table_name='Patients';
go