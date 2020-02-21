using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watsonia.Data.Tests.Cache.Entities;

namespace Watsonia.Data.Tests.Cache
{
	public partial class CacheTests
	{
		[TestMethod]
		public void ValueBagClasses()
		{
			// Check that the teacher bag has all of its properties
			var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("Watsonia.Data.DynamicProxies,"));
			var teacherBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseTeacherValueBag");
			var teacherBag = (IValueBag)teacherBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(teacherBag);

			var teacherProperties = teacherBagType.GetProperties();
			Assert.AreEqual(7, teacherProperties.Length);
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "FirstName"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "LastName"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "Email"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "DateOfBirth"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "Age"));
			Assert.IsTrue(teacherProperties.Any(p => p.Name == "Rating"));

			// Check that the class bag has all of its properties
			var classBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseClassValueBag");
			var classBag = classBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(classBag);

			var classProperties = classBagType.GetProperties();
			Assert.AreEqual(3, classProperties.Length);
			Assert.IsTrue(classProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(classProperties.Any(p => p.Name == "Name"));
			Assert.IsTrue(classProperties.Any(p => p.Name == "TeacherID"));

			// Check that the student bag has all of its properties
			var studentBagType = assembly.GetType("Watsonia.Data.DynamicProxies.CacheDatabaseStudentValueBag");
			var studentBag = studentBagType.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes);
			Assert.IsNotNull(studentBag);

			var studentProperties = studentBagType.GetProperties();
			Assert.AreEqual(4, studentProperties.Length);
			Assert.IsTrue(studentProperties.Any(p => p.Name == "ID"));
			Assert.IsTrue(studentProperties.Any(p => p.Name == "Name"));
			Assert.IsTrue(studentProperties.Any(p => p.Name == "Age"));
			Assert.IsTrue(studentProperties.Any(p => p.Name == "ClassID"));

			// Check __SetValuesFromBag
			var teacher = _db.Create<Teacher>();
			teacherProperties.First(p => p.Name == "ID").SetValue(teacherBag, 25);
			teacherProperties.First(p => p.Name == "FirstName").SetValue(teacherBag, "Dan");
			teacherProperties.First(p => p.Name == "LastName").SetValue(teacherBag, "Brown");
			teacherProperties.First(p => p.Name == "Email").SetValue(teacherBag, "dan.brown@example.com");
			teacherProperties.First(p => p.Name == "DateOfBirth").SetValue(teacherBag, new DateTime(1960, 1, 1));
			teacherProperties.First(p => p.Name == "Age").SetValue(teacherBag, null);
			teacherProperties.First(p => p.Name == "Rating").SetValue(teacherBag, 10);
			var teacherProxy = (IDynamicProxy)teacher;
			teacherProxy.__SetValuesFromBag(teacherBag);
			Assert.AreEqual(25, (long)teacherProxy.__PrimaryKeyValue);
			Assert.AreEqual("Dan", teacher.FirstName);
			Assert.AreEqual("Brown", teacher.LastName);
			Assert.AreEqual("dan.brown@example.com", teacher.Email);
			Assert.AreEqual(new DateTime(1960, 1, 1), teacher.DateOfBirth);
			Assert.AreEqual(null, teacher.Age);
			Assert.AreEqual(10, teacher.Rating);

			// Check __GetBagFromValues
			var teacherBag2 = teacherProxy.__GetBagFromValues();
			Assert.AreEqual(25, (long)teacherProperties.First(p => p.Name == "ID").GetValue(teacherBag2));
			Assert.AreEqual("Dan", teacherProperties.First(p => p.Name == "FirstName").GetValue(teacherBag2));
			Assert.AreEqual("Brown", teacherProperties.First(p => p.Name == "LastName").GetValue(teacherBag2));
			Assert.AreEqual("dan.brown@example.com", teacherProperties.First(p => p.Name == "Email").GetValue(teacherBag2));
			Assert.AreEqual(new DateTime(1960, 1, 1), teacherProperties.First(p => p.Name == "DateOfBirth").GetValue(teacherBag2));
			Assert.AreEqual(null, teacherProperties.First(p => p.Name == "Age").GetValue(teacherBag2));
			Assert.AreEqual(10, teacherProperties.First(p => p.Name == "Rating").GetValue(teacherBag2));
		}
	}
}
