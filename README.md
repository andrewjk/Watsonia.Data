# Watsonia.Data: A .NET Object-Relational Mapper #

Watsonia.Data is a simple object-relational mapper designed to be dropped into a project and work with your entity classes with a minimum of fuss.  It provides the following functionality:

- Create and update a database schema from your entity classes
- Configurable mapping of entity classes to database tables
- Load and save entity objects from the database with automatic change tracking
- Load entities with LINQ, a fluent SQL API or (if you must...) with SQL strings
- Update and delete entities in bulk
- Lazy loading of entity properties that contain related items or collections, with eager loading via the Include method
- Automatic validation of entities that are decorated with System.ComponentModel.DataAnnotations.ValidationAttributes or that implement IValidatableObject
- Hook most database operations with event handlers or method overrides
- Support for database transactions
- Built-in support for Microsoft SQL Server and SQLite, with other databases via a plugin architecture
- Sync and async APIs, including cancellation and streaming async results

Watsonia.Data is pre-1.0. The core query and persistence paths are covered by a cross-platform test suite; it is not intended to replace feature-complete ORMs such as Entity Framework. It targets **netstandard2.0** and **net10.0**.

## Installation ##

```bash
dotnet add package Watsonia.Data
dotnet add package Watsonia.Data.SQLite      # or: Watsonia.Data.SqlServer
dotnet add package Watsonia.Data.Extensions   # optional: DI / logging / tracing / caching
```

## Getting Started ##

So you've added a reference to Watsonia.Data to your assembly?  And you have some entity classes you'd like to map to a database?  Great!  The first step is to ensure that the properties you want to map to database columns are virtual and settable.  For example, in the following entity class, the FirstName, LastName and Rating properties will be loaded from the database and FullName will be ignored:  

```C#
public class Author
{
	public virtual string FirstName
	{
		get;
		set;
	}

	public virtual string LastName
	{
		get;
		set;
	}

	public string FullName
	{
		get
		{
			return string.Format("{0} {1}", this.FirstName, this.LastName);
		}
	}

	public virtual int Rating
	{
		get;
		set;
	}
}
```

Entities are loaded and saved via the Database class.  The quickest way to get up and running with this is by creating a new instance of Database and passing in the connection string and the namespace in which your entities reside:  

```C#
var db = new Watsonia.Data.Database(
	new Watsonia.Data.SQLite.SQLiteDataAccessProvider(),
	"Data Source=app.db",
	"MyApp.Entities");
```

Use `Watsonia.Data.SqlServer.SqlServerDataAccessProvider` for SQL Server.

If you want to get a bit more fancy (e.g. if you want to determine which entities to map based on something other than namespace or you want to map entities to tables with different names) you can create a new instance of Database and pass in a DatabaseConfiguration parameter.  See the Database Mapping section below for more information.  

## Loading Entities ##

Entities can be loaded with LINQ, a fluent SQL API or SQL strings.  Using LINQ to select all of the authors with last names that start with "P" looks like this:

```C#
var query = from a in db.Query<Author>()
			where a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase)
			select a;
foreach (Author a in query)
{
	Console.WriteLine(a.FullName);
}
```

Using the fluent SQL API to do the same looks like this:

```C#
var query = Select.From<Author>().Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
foreach (Author a in db.LoadCollection(query))
{
	Console.WriteLine(a.FullName);
}
```

or like this:

```C#
var query = Select.From("Author").Where("LastName", SqlOperator.StartsWith, "P");
foreach (Author a in db.LoadCollection<Author>(query))
{
	Console.WriteLine(a.FullName);
}
```

The fluent SQL API includes support for most standard SQL operations including joins, grouping and paging.  

Using SQL to do the same looks like this (for an SQL Server database):

```C#
var query = "SELECT * FROM Author WHERE LastName LIKE '%' + @0 + '%'";
foreach (Author a in db.LoadCollection<Author>(query, "P"))
{
	Console.WriteLine(a.FullName);
}
```

You can also load a scalar value using LINQ or fluent SQL:

```C#
var query = Select.From("Author").Count("*").Where("LastName", SqlOperator.StartsWith, "P");
int count = (int)db.LoadValue(query);
```

Or load a single entity with its primary key value:

```C#
var author = db.Load<Author>([AuthorID]);
```

Most load operations have async counterparts (`LoadAsync`, `LoadCollectionAsync`, `LoadValueAsync`, or `ToListAsync` on LINQ queries); the token-aware overloads accept a `CancellationToken`.

## Saving Entities ##

After you've loaded your entities you can make changes to their properties and save them back to the database:

```C#
var author = db.Load<Author>([AuthorID]);
author.Rating = 80;
db.Save(author);
```

To create an entity you must use the Database.Create method to obtain a proxy object that Watsonia.Data can track and save.  If you try to save a non-proxy object an exception will be raised.  The following code will successfully create a new author:

```C#
var newAuthor = db.Create<Author>();
newAuthor.FirstName = "Eric";
newAuthor.LastName = "Blair";
db.Save(newAuthor);
```

As will the following, slightly more concise version:

```C#
var newAuthor = db.Insert(new Author() { FirstName = "Eric", LastName = "Blair" });
```

The following code will raise an exception though:

```C#
var author = new Author();
author.FirstName = "Eric";
author.LastName = "Blair";
db.Save(author);
```

Sorry :(

## Bulk Update and Delete ##

You can update entities in bulk using fluent SQL methods or SQL strings:

```C#
var update = Update.Table<Author>().Set(a => a.Rating, 95).Where(a => a.LastName.StartsWith("P", StringComparison.InvariantCultureIgnoreCase));
db.Execute(update);
```

```C#
var update2 = Update.Table("Author").Set("Rating", 95).Where("LastName", SqlOperator.StartsWith, "P");
db.Execute(update2);
```

```C#
var update3 = "UPDATE Author SET Rating = 95 WHERE LastName LIKE @0 + '%'";
db.Execute(update3, "P");
```

You can also delete entities in bulk using fluent SQL methods or SQL strings:

```C#
var delete = Delete.From<Author>().Where(a => a.Rating < 80);
db.Execute(delete);
```

```C#
var delete2 = Delete.From("Author").Where("Rating", SqlOperator.IsLessThan, 80);
db.Execute(delete2);
```

```C#
var delete3 = "DELETE FROM Author WHERE Rating < @0";
db.Execute(delete3, 80);
```

## Database Mapping ##

Out of the box, Watsonia.Data uses these conventions when mapping entities to the database:

- Each entity class is mapped to a database table with the same name as the class
- Each entity property is mapped to a database column with the same as the property
- The primary key column is a 64 bit integer named "ID" by default (override `GetPrimaryKeyColumnName`/`GetPrimaryKeyColumnType` for string or other key types)
- Entities are mapped if they exist in the namespace passed into the Database class's constructor
- Properties are mapped if they are readable, writeable, public and virtual
- If a property is of an entity type it is considered to be a related item and will be lazily loaded and linked by its foreign key
- If a property is of an enumerable entity type (i.e. IEnumerable<T> where T is an entity type) it is considered to be a related collection and will be lazily loaded AND saved with the entity

Any of these conventions may be overridden by creating a new class that inherits from Watsonia.Data.DatabaseConfiguration and passing it into the constructor to Database, e.g.:

```C#
using Watsonia.Data;
using Watsonia.Data.SQLite;

public class MyConfiguration : DatabaseConfiguration
{
	public MyConfiguration()
		: base(new SQLiteDataAccessProvider(), "Data Source=app.db", "MyApp.Entities")
	{
	}

	public override string GetTableName(Type type)
	{
		return type.Name + "s";
	}

	...
}
```

```C#
var config = new MyConfiguration();
var db = new Watsonia.Data.Database(config);
```

## Lazy and Eager Loading ##

Related items and collections are loaded on first access by default:

```C#
var author = db.Load<Author>([AuthorID]);
var books = author.Books;        // lazily loaded
var publisher = author.Publisher; // lazily loaded
```

Eager-load them with Include:

```C#
var authors = db.Query<Author>().Include(a => a.Books).ToList();
```

Async queries and loads are available too:

```C#
var authors = await db.Query<Author>()
	.Where(a => a.LastName.StartsWith("P"))
	.ToListAsync(cancellationToken);

var books = await db.LoadCollectionAsync(author, a => a.Books, cancellationToken);
var publisher = await db.LoadItemAsync(author, a => a.Publisher, cancellationToken);
```

Set `LazyLoadingEnabled = false` on the `DatabaseConfiguration` to prevent implicit lazy loading; accessing an unloaded related item then throws an exception that tells you to use Include or an explicit loader.

## Cascading Save and Delete Operations ##

Related items and collections are saved with their parent by default. Opt out with `[Cascade(ShouldCascade = false)]` on the property (from `Watsonia.Data.DataAnnotations`). A related item that is not cascaded must be saved before the parent.

Cascading delete is opt-in: mark the relationship with `[CascadeDelete]` if items removed from a loaded collection, or children of a deleted parent, should also be deleted.

## Updating the Database Schema ##

You can update the database by calling the database's UpdateDatabase method:

```C#
db.UpdateDatabase();
```

If you just want to see what will be updated you can call GetUpdateScript instead:

```C#
string script = db.GetUpdateScript();
```

When updating the database:

- Table and column names are taken from the configuration object that is used for the database
- By default:
	- Tables have the same name as the class
	- Columns have the same name as the property
	- The primary key column is named ID
	- Foreign key columns are named [ForeignTable]ID
- Property types are mapped to the appropriate column types for the database server used
- Columns are nullable if the property is of a reference type or nullable value type
- Strings will not be considered nullable!
- The length of string columns is read from a StringLength attribute if present or defaulted to 255
- Default values are read from a DefaultValue attribute if present or the CLR default of the property type
- Enums are created as a table and any properties that are of enum types are created as foreign key columns
- Columns and tables are not deleted from the database when classes or properties are removed
- Columns and tables are not renamed, instead a new column or table will be created

Schema updates are additive; there are no migration snapshots or script runners yet, so changing existing production schemas still needs a manual migration plan.

## How it Works ##

Watsonia.Data creates a proxy object for each entity class when its corresponding database table is accessed.  The proxy object is an instance of a class that inherits from the entity class and overrides its virtual properties.  In this way we can intercept changes to property values to provide change notification and validation functionality.

Each proxy object class implements the interface Watsonia.Data.IDynamicProxy, which exposes a `StateTracker` with the change-tracking, validation and lazy-loading state (including `IsNew`, `HasChanges`, `IsValid` and `ValidationErrors`). Proxy equality and hash codes are based on the entity type and primary key.

## Validation ##

Each proxy object class provides validation error information.  When the proxy object is loaded or created, it will have no errors.  When each property is set by the user, any System.ComponentModel.DataAnnotations.ValidationAttributes will be checked and errors created if the property value is invalid.  When the proxy object is saved to the database, all properties will be checked to see whether they are valid and an exception will be thrown if any are invalid.  For example, we can change the author class to look like this:

```C#
public class Author
{
	[Required]
	public virtual string FirstName
	{
		get;
		set;
	}

	[Required]
	public virtual string LastName
	{
		get;
		set;
	}

	...
}
```

and then create an author:

```C#
Author author = db.Create<Author>();
```

At this point the author will be considered to have no errors.  If we attempt to save the author or check the author's IsValid property, however, the author will have two errors in its ValidationErrors collection.  

You can also implement the IValidatableObject interface if you have more complex validation requirements and it will be called when saving to the database or checking IsValid.  

Note that validation is recursive and will check any loaded related items or collections that are contained in an entity's properties.  We can add a collection of books to the author class:

```C#
public class Author
{
	...

	public virtual IList<Book> Books
	{
		get;
		set;
	}

	...
}

public class Book
{
	[Required]
	public virtual string Title
	{
		get;
		set;
	}
}
```

If we then add a book with no title to the author, attempting to save the author will throw a `ValidationException`; the author proxy's `StateTracker.ValidationErrors` will contain the missing title error.

## Other Functionality ##

Each proxy exposes its tracking state through `((IDynamicProxy)author).StateTracker`, including `IsNew`, `HasChanges`, `IsValid` and `ValidationErrors`.

## Transactions ##

Sometimes you may want to run a bunch of operations against the database and undo your changes if any of the operations fail.  The following code will attempt to change each Author's rating and only commit the operation to the database if every save completes successfully:

```C#
using (DbConnection connection = db.OpenConnection())
using (DbTransaction transaction = connection.BeginTransaction())
{
	try
	{
		foreach (Author a in authors)
		{
			a.Rating = 50;
			db.Save(a, connection, transaction);
		}

		transaction.Commit();
	}
	catch
	{
		transaction.Rollback();
		throw;
	}
}
```

## License

Watsonia.Data is released under the [MIT License](http://opensource.org/licenses/MIT).

Third-party attributions and license texts (IQToolkit/MS-PL, Entity Framework Core/MIT, Northwind sample data, benchmark inspiration) are in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
