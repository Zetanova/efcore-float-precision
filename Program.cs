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
                Value = 1.03d
            };

            Console.WriteLine($"value {test1.Value} saving");

            context.Tests.Add(test1);

            await context.SaveChangesAsync(cts.Token);

            //clear to reload the entity
            context.ChangeTracker.Clear();


            var test1Loaded = await context.Tests.FindAsync(new object[] { test1.Id }, cts.Token);

            Console.WriteLine($"value {test1Loaded!.Value} loaded");


            //set value to source
            test1Loaded.Value = test1.Value;

            context.ChangeTracker.DetectChanges();

            if(context.ChangeTracker.HasChanges())
                Console.WriteLine($"value changed between round trip");


            //result output
            /*
                value 1,03 saving
                value 1,0299999713897705 loaded
                value changed between round trip
            */
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
        public double Value { get; set; }
    }
}