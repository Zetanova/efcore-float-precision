using Microsoft.EntityFrameworkCore;

namespace efcore_float_precision
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("abort");
                cts.Cancel();
                e.Cancel = true;
            };

            var contextOptions = new DbContextOptionsBuilder<TestContext>()
                .UseSqlServer("Server=localhost;Database=efcore-precision-test;User=sa;Password=YOURPASSWORD;TrustServerCertificate=true;")
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;

                        
            await using var context = new TestContext(contextOptions);

            await context.Database.EnsureDeletedAsync(cts.Token);
            await context.Database.EnsureCreatedAsync(cts.Token);

            var test1 = new TestEntity
            {
                Value1 = 1.03d,
                Value2 = 1.04d,
            };

            Console.WriteLine($"value {test1.Value1} saving");

            context.Tests.Add(test1);

            await context.SaveChangesAsync(cts.Token);

            //clear to reload the entity
            context.ChangeTracker.Clear();


            var test1Loaded = await context.Tests.FindAsync(new object[] { test1.Id }, cts.Token);

            Console.WriteLine($"value {test1Loaded!.Value1} loaded");


            //set value to source
            test1Loaded.Value1 = test1.Value1;

            context.ChangeTracker.DetectChanges();

            if(context.ChangeTracker.HasChanges())
                Console.Error.WriteLine($"value changed between round trip");

            //result output
            /*
                value 1,03 saving
                value 1,0299999713897705 loaded
                value changed between round trip
            */

            {
                Console.WriteLine("reading nullable value2 over projection without default");
                var items = await context.Tests.AsNoTracking()
                    .Select(n => new TestDto
                    {
                        Value = n.Value2
                    })
                    .ToListAsync(cts.Token);
            }

            {
                Console.WriteLine("reading nullable value over projection with default");
                var items = await context.Tests.AsNoTracking()
                    .Select(n => new TestDto
                    {
                        Value = n.Value2 ?? 0
                    })
                    .ToListAsync(cts.Token);

                /*
                 System.InvalidOperationException
                      HResult=0x80131509
                      Message=An error occurred while reading a database value. The expected type was 'System.Nullable`1[System.Double]' but the actual value was of type 'System.Double'.
                      Source=Microsoft.EntityFrameworkCore.Relational
                      StackTrace:
                       at Microsoft.EntityFrameworkCore.Query.RelationalShapedQueryCompilingExpressionVisitor.ShaperProcessingExpressionVisitor.ThrowReadValueException[TValue](Exception exception, Object value, Type expectedType, IPropertyBase property)
                       at Microsoft.EntityFrameworkCore.Query.Internal.SingleQueryingEnumerable`1.AsyncEnumerator.<MoveNextAsync>d__20.MoveNext()
                       at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.<ToListAsync>d__65`1.MoveNext()
                       at Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.<ToListAsync>d__65`1.MoveNext()
                       at efcore_float_precision.Program.<Main>d__0.MoveNext() in M:\projects\efcore-float-precision\Program.cs:line 77
                       at efcore_float_precision.Program.<Main>d__0.MoveNext() in M:\projects\efcore-float-precision\Program.cs:line 83

                      This exception was originally thrown at this call stack:
                        [External Code]

                    Inner Exception 1:
                    InvalidCastException: Unable to cast object of type 'System.Double' to type 'System.Single'.

                */
            }
        }
    }

    public sealed class TestContext : DbContext
    {
        public DbSet<TestEntity> Tests => Set<TestEntity>();

        public TestContext()
        {
        }

        public TestContext(DbContextOptions<TestContext> options) :
            base(options)
        {
        }
    }

    public sealed class TestEntity
    {
        public int Id { get; set; }

        [Precision(2)]
        public double Value1 { get; set; }

        [Precision(2)]
        public double? Value2 { get; set; } //nullable
    }

    public sealed class TestDto
    {
        public double? Value { get; set; }
    }
}