using NUnit.Framework;
using Retail.Employee.Upsert.Common.Services;
using Retail.Employee.Upsert.Common.Models;

namespace Retail.Employee.Upsert.Common.Tests
{
    [TestFixture]
    public class TransformationServiceTests
    {
        [Test]
        public void IsValidData_ValidUser_ReturnsTrue()
        {
            // Arrange
            var transformationService = new TransformationService();
            var retailEmployee = new RetailEmployee
            {
                Person = new Person
                {
                    PreferredFirstName = "John",
                    LastName = "Doe"
                },
                Employment = new Employment
                {
                    CompanyCode = "US",
                    LocationType = "Store",
                    JobCode = "12345",
                    StoreNumber = "123"
                }
            };
            var appSettings = new AppSettings
            {
                NorthAmericaLegalEntities = new string[] { "US" }
            };
            string warningMessageUser = "";
            string warningMessageCustomer = "";

            // Act
            var result = transformationService.IsValidData(retailEmployee, appSettings, ref warningMessageUser, ref warningMessageCustomer);

            // Assert
            Assert.IsTrue(result.Item1);
            Assert.IsTrue(result.Item2);
            Assert.AreEqual("", warningMessageUser);
            Assert.AreEqual("", warningMessageCustomer);
        }
    }
}