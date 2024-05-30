﻿using System.Data.SqlClient;
using System.Reflection;

ApplicationDbContext context = new();

var list = context.Products.ToList();

Console.ReadLine();

record Product
{
    public Product()
    {

    }
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

class ApplicationDbContext : MyContext
{
    public override void OnConfiguring(MyContextBuilder builder)
    {
        builder.UseSqlServer("Data Source=TANER\\SQLEXPRESS;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");
    }
    public DbSet<Product> Products { get; set; } = default!;
}

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

        string? tableName = _tableName;

        if (tableName is null)
        {
            throw new ArgumentException("Table not found");
        }

        SqlCommand cmd = new("Select * from " + tableName, con);
        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {

                object[] parameters = new object[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    parameters[i] = reader.GetValue(i);
                }

                // T türünde bir nesne oluşturun
                T entity = (T)Activator.CreateInstance(typeof(T), parameters);

                list.Add(entity);
            }
        }

        con.Close();

        return list;
    }
}



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

    public MyContext(string connectionString)
    {
        Builder = new MyContextBuilder();
        Builder.ConnectionString = connectionString;
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
            var tableName = entityType.Name + "s"; // Basit bir tablo adı belirleme mantığı, özelleştirilebilir
            var dbSetType = typeof(DbSet<>).MakeGenericType(entityType);
            var dbSetInstance = Activator.CreateInstance(dbSetType, Builder.ConnectionString, tableName);
            property.SetValue(this, dbSetInstance);
        }
    }
}