# Kendi ORM'iniz (Object-Relational Mapper)

Bu proje, EF Core tarz�nda s�f�rdan bir ORM (Object-Relational Mapper) yazmay� g�steren bir e�itim ama�l� projedir. Bu ORM, Read i�lemini ger�ekle�tirebilmenizi sa�lar.

## Proje �zeti

Bu projede, bir veritaban� ba�lam� (`DbContext`) ve varl�k setleri (`DbSet`) olu�turma ve y�netme i�lemlerini g�steriyoruz. �rnek bir `Product` s�n�f� kullanarak veritaban�ndan veri �ekme i�lemini ger�ekle�tirdik.

### Koddan �rnekler

#### Product S�n�f�

```csharp
record Product
{
    public Product() { }

    public Product(int id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
}
````

#### ApplicationDbContext S�n�f�

```csharp
class ApplicationDbContext : MyContext
{
    public override void OnConfiguring(MyContextBuilder builder)
    {
        builder.UseSqlServer("Data Source=TANER\\SQLEXPRESS;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
    }
    public DbSet<Product> Products { get; set; } = default!;
}
````

#### DbSet S�n�f�

```csharp
class DbSet<T>
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public DbSet(string connectionstring, string tableName)
    {
        _connectionString = connectionstring;
        _tableName = tableName;
    }

    public List<T> ToList()
    {
        List<T> list = new List<T>();

        SqlConnection con = new(_connectionString);
        con.Open();

        SqlCommand cmd = new("Select * from " + _tableName, con);
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                object[] parameters = new object[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    parameters[i] = reader.GetValue(i);
                }

                T entity = (T)Activator.CreateInstance(typeof(T), parameters);
                list.Add(entity);
            }
        }

        con.Close();
        return list;
    }
}
````

#### MyContext ve MyContextBuilder S�n�flar�

```csharp
class MyContextBuilder
{
    public string ConnectionString { get; set; } = default!;

    public void UseSqlServer(string connectionString)
    {
        ConnectionString = connectionString;
    }
}

class MyContext
{
    public MyContextBuilder Builder { get; set; } = new();

    public MyContext()
    {
        OnConfiguring(Builder);
        InitializeDbSets();
    }

    public virtual void OnConfiguring(MyContextBuilder builder) { }

    private void InitializeDbSets()
    {
        var properties = this.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var property in properties)
        {
            var entityType = property.PropertyType.GetGenericArguments().First();
            var tableName = entityType.Name + "s"; // Basit bir tablo ad� belirleme mant���, �zelle�tirilebilir
            var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
            var dbSetInstance = Activator.CreateInstance(dbSetType, Builder.ConnectionString, tableName);
            property.SetValue(this, dbSetInstance);
        }
    }
}
````

## Kullan�m

Projenin nas�l kullan�laca��n� g�stermek i�in a�a��daki �rnek kodu inceleyebilirsiniz:

```csharp
ApplicationDbContext context = new();
var list = context.Products.ToList();
Console.ReadLine();
```


## Buraya kadar okuduysan�z Repoyu y�ld�zlayarak destek verebilirsiniz :)
