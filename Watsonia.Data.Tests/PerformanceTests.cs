using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Tests.PerformanceModels;

namespace Watsonia.Data.Tests
{
#if !DEBUG
	[TestClass]
#endif
	public class PerformanceTests
	{
		public const string ConnectionString = @"Data Source=Data\PerformanceTests.sdf;Persist Security Info=False";

		private static Database db = new Database(PerformanceTests.ConnectionString, "Watsonia.Data.Tests.PerformanceModels");

#if !DEBUG
		[ClassInitialize]
#endif
		public static void Initialize(TestContext context)
		{
			if (!File.Exists(@"Data\PerformanceTests.sdf"))
			{
				File.Create(@"Data\PerformanceTests.sdf");
			}

			db.Configuration.ProviderName = "Watsonia.Data.SqlServerCe";
			db.UpdateDatabase();

			if (db.Query<Post>().Count() == 0)
			{
				for (int i = 0; i < 5000; i++)
				{
					// TODO: I guess this would be a good candidate for fluent inserts
					Post post = db.Create<Post>();
					post.Text = new string('x', 2000);
					post.CreationDate = DateTime.Now;
					post.LastChangeDate = DateTime.Now;
					db.Save(post);
				}
			}
		}

#if !DEBUG
		[TestMethod]
#endif
		public void TestLoadingPostPerformance()
		{
			var tests = new Tests();

			// These tests are pretty much identical to the Dapper tests
			tests.Add(id => db.Load<Post>(id), "Watsonia.Data Load");
			tests.Add(id => db.LoadCollection<Post>(@"SELECT * FROM Post WHERE ID = @0", id), "Watsonia.Data SQL");
			tests.Add(id => db.LoadCollection<Post>(Select.From("Post").Where("ID", SqlOperator.Equals, id)), "Watsonia.Data Fluent SQL");
			tests.Add(id => db.LoadCollection<Post>(Select.From<Post>().Where(p => p.ID == id)), "Watsonia.Data Fluent Expressions");
			tests.Add(id => db.Query<Post>().Where(p => p.ID == id).First(), "Watsonia.Data LINQ");

			const int iterations = 200;
			tests.Run(iterations);

			StringBuilder b = new StringBuilder();
			foreach (var test in tests.OrderBy(t => t.Watch.ElapsedMilliseconds))
			{
				b.AppendLine(test.Name + ": " + test.Watch.ElapsedMilliseconds + "ms");
			}

			// Write it to a results file so that we can have a look at it over time
			using (StreamWriter writer = new StreamWriter(@"Data\PerformanceLog.txt", true))
			{
				writer.Write(DateTime.Now);
				writer.Write("\t");
				for (int i = 0; i < tests.Count; i++)
				{
					writer.Write(tests[i].Watch.ElapsedMilliseconds);
					writer.Write("\t");
				}
				writer.WriteLine();
			}

			foreach (var test in tests.OrderBy(t => t.Watch.ElapsedMilliseconds))
			{
				Assert.IsTrue(test.Watch.ElapsedMilliseconds < 10000, b.ToString());
			}
		}

		class Test
		{
			public static Test Create(Action<int> iteration, string name)
			{
				return new Test { Iteration = iteration, Name = name };
			}

			public Action<int> Iteration { get; set; }
			public string Name { get; set; }
			public Stopwatch Watch { get; set; }
		}

		class Tests : List<Test>
		{
			public void Add(Action<int> iteration, string name)
			{
				Add(Test.Create(iteration, name));
			}

			public void Run(int iterations)
			{
				// warmup 
				foreach (var test in this)
				{
					test.Iteration(iterations + 1);
					test.Watch = new Stopwatch();
					test.Watch.Reset();
				}

				var rand = new Random();
				for (int i = 1; i <= iterations; i++)
				{
					foreach (var test in this.OrderBy(ignore => rand.Next()))
					{
						test.Watch.Start();
						test.Iteration(i);
						test.Watch.Stop();
					}
				}

				foreach (var test in this.OrderBy(t => t.Watch.ElapsedMilliseconds))
				{
					Console.WriteLine(test.Name + " took " + test.Watch.ElapsedMilliseconds + "ms");
				}
			}
		}
	}
}
