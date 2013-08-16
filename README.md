Joe.Business
============

Repository Framework that builds upon Joe.Map and Joe.MapBack

Use this create Repositories for each of Your Context's DbSets

The Recommeded Pattern for Creating a Repository is

```
public class Person
{
  public int Id { get; set; }
  public String Name { get; set; }
}

//MapBackDbContext is just an Extentsion of DbContext that implements the interface IDbViewContext
//This allows for any type of Object serving as a context to work with Joe.Business,
//you just have to Implment IDbViewContext
public class Context : Joe.MapBack.MapBackDbContext
{
  public DbSet<Person> People { get; set; }
}

public class PersonRepository<TViewModel, TContext> : Joe.Business.Repository<Person, TViewModel, TContext>
  where TViewModel : class, new()
	where TContext : class, Joe.MapBack.IDBViewContext, new()
	{
	}
```
###Use
When you create a repository that inherits from Joe.Business.Repository it implemnets all the default CRUD methods for you.
In your UI you code might look something like this.

```
var personRepo = new PersonRepository<PersonView, Context>();

//Get List of Persons
var people = personRepo.Get();

//Get Person By ID
var person = personRepo.Get(1);

//Delete person either by id or by Person View
personRepo.Delete(1); /*or*/ personRepo.Delete(person);

//Create a new Person by creating a new PersonView and passing it in
var newPerson = new PersonView(){ Name = "Joe" };
newPerson = personRepo.Create(newPersion);

//Update Person
newPerson.Name = "John";
personRepo.Update(newPerson);


```

###Internal API
The internal API consists of Several delegates you can tie into
```
protected delegate void MapDelegate(TModel model, TViewModel viewModel, TContext repository);
protected delegate void SaveDelegate(TModel model, TViewModel viewModel, TContext repository);
protected delegate void AfterGetDelegate(TViewModel viewModel, TContext repository);
protected delegate void BeforeGetDelegate(TContext repository);

protected SaveDelegate BeforeUpdate;
protected SaveDelegate BeforeDelete;
protected SaveDelegate BeforeCreate;
protected SaveDelegate AfterUpdate;
protected SaveDelegate AfterDelete;
protected MapDelegate BeforeMapBack;
protected MapDelegate AfterMap;
protected SaveDelegate AfterCreate;
protected BeforeGetDelegate BeforeGet;
protected AfterGetDelegate AfterGet;
protected GetListDelegate BeforeReturnList;
```

These will mostly be used to implement business rules. Lets look at a simple example
```
public class PersonRepository<TViewModel, TContext> : Joe.Business.Repository<Person, TViewModel, TContext>
  where TViewModel : class, new()
	where TContext : class, Joe.MapBack.IDBViewContext, new()
	{
	  public PersonRepository()
	  {
	    this.AfterCreate += WhenSavedCreateAnotherPersion;
	    this.AfterSave += WhenSavedCreateAnotherPersion;
	  }
	
	  public void WhenSavedCreateAnotherPersion(Person model, TViewModel viewModel, TContext context)
	  {
	    var personDbSet = context.GetIDbSet<Person>();
	    Person anotherPerson = dbSet.Create();
	    anotherPerson.Name = "Jim";
	    dbSet.Add(anotherPerson);
	  }
	}
```

###MapRepoFunction
The Map Repo Function consists of several Attributes that make it much easier to create ViewModels to be used for your UI

__MapRepoFunction__- `MapRepoFunctionAttribute(String function, params String [] propertiesToPassIn)` This allows you Map a
property in your ViewModel to a function in your Repository. This might be down for complex Calculations that need to be done
in code. Here is a simple Example of this:

```
public class PersonView 
{
  public int ID { get; set; }
  public String Name { get; set; }
  
  [MapRepoFunction("ReverseName", "Name")]
  public Sting NameBackwards { get; set; }
  
}
  
  //The Repository that you are calling to generate to get the data
  public class PersonRepository<TViewModel, TContext> : Joe.Business.Repository<Person, TViewModel, TContext>
  where TViewModel : class, new()
	where TContext : class, Joe.MapBack.IDBViewContext, new()
	{
	  public String ReverseName(String name)
	  {
	    return name.Reverse();
	  }
	}
}
```

__AllValues__ - `AllValues(Type repoToInvoke, Type model'/'AllValues(Type Model)` This lets you map a property in your view
to All the Values of another Entity Object Lets assume that the Person Entity we have been working with has a One To Many
relation with Jobs. Our Person Entity might look like such:

```
public class Person
{
  public int Id { get; set; }
  public String Name { get; set; }

  public int JobId { get; set; }
  public virtural Job Job { get; set; }
}
```

In your ViewModel you can do this to have a list of all possible Jobs for your Job Drop Down

```
public class PersonView
{
  public int Id { get; set; }
  public String Name { get; set; }
  
  public int JobId { get; set; }
  
  //This will pull and Map the Job Entity Objects to the JobView directly from the Context
  [AllValues(typeof(Job))]
  public IEnumerable<JobView> AllJobs { get; set; }
  
  //This will pull and Map the Job Entity Objects to the JobView using the JobRepository
  [AllValues(typeof(JobRepository<,>), typeof(Job))]
  public IEnumerable<JobView> AllJobs { get; set; }
}
```

More To Come Soon...

