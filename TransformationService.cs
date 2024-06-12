namespace Retail.Employee.Upsert.Common.Services
{
    using Retail.OData.Client;

    public class TransformationService
    {
        /// <summary>
        /// To check valid user or not
        /// </summary>
        /// <param name="user">user details from AAD</param>
        /// <returns>Return valid user - True/False</returns>

        public (bool, bool) IsValidData(Models.RetailEmployee retailEmployee, Models.AppSettings appSettings, ref string warningMessageUser, ref string warningMessageCustomer)
        {
            var validuser = true;
            var validcustomer = true;
            if (retailEmployee == null)
            {
                validuser = false;
                validcustomer = false;
                warningMessageUser = "UserInformation is null.";
                warningMessageCustomer = "CustomerInformation is null.";
                return (validuser, validcustomer);
            }
            else if (string.IsNullOrEmpty(retailEmployee.Person.PreferredFirstName) && string.IsNullOrEmpty(retailEmployee.Person.FirstName))
            {
                validuser = false;
                validcustomer = false;
                warningMessageUser = "UserInformation: GivenName is null.";
                warningMessageCustomer = "CustomerInformation: GivenName is null.";
                return (validuser, validcustomer);
            }
            else if (string.IsNullOrEmpty(retailEmployee.Person.PreferredLastName) && string.IsNullOrEmpty(retailEmployee.Person.LastName))
            {
                validuser = false;
                validcustomer = false;
                warningMessageUser = "UserInformation: Surname is null.";
                warningMessageCustomer = "CustomerInformation: Surname is null.";
                return (validuser, validcustomer);
            }

            if (!IsNorthAmerican(retailEmployee, appSettings) && !IsEuropean(retailEmployee, appSettings))
            {
                validuser = false;
                warningMessageUser = $"UserInformation: Invalid Country code:{retailEmployee.Employment.CompanyCode}.";
            }
            else if (!retailEmployee.Employment.LocationType.EqualsIgnoreCase("Store") && !appSettings.LocationTypeJobCodeException.Split(",").Contains(retailEmployee.Employment.JobCode))
            {
                validuser = false;
                warningMessageUser = $"UserInformation: Invalid LocationType: {retailEmployee.Employment.LocationType}.";
            }
            else if (string.IsNullOrEmpty(retailEmployee.Employment.StoreNumber) && !appSettings.StoreNumberJobCodeException.Split(",").Contains(retailEmployee.Employment.JobCode))
            {
                validuser = false;
                warningMessageUser = "UserInformation: StoreNumber is null.";
            }

            if (!IsNorthAmerican(retailEmployee, appSettings) && !appSettings.CompanyCodeException.Split(",").Contains(retailEmployee.Employment.CompanyCode))
            {
                validcustomer = false;
                warningMessageCustomer = $"CustomerInformation: Invalid Country code:{retailEmployee.Employment.CompanyCode}.";
            }
            else if (!(appSettings.LocationTypeCompanyCodeException.Split(",").Contains(retailEmployee.Employment.CompanyCode) && (retailEmployee.Employment.LocationType.EqualsIgnoreCase("Store") || retailEmployee.Employment.LocationType.EqualsIgnoreCase("Office"))) && !IsNorthAmerican(retailEmployee, appSettings))
            {
                validcustomer = false;
                warningMessageCustomer = $"CustomerInformation: Invalid LocationType: {retailEmployee.Employment.LocationType} for LegalEntity: {retailEmployee.Employment.CompanyCode}.";
            }
            return (validuser, validcustomer);
        }

        /// <summary>
        /// Process Exceptions scenarios for jobcodes
        /// </summary>
        /// <param name="retailEmployee"></param>
        /// <param name="humanResourceMapping"></param>
        /// <param name="exceptionJobCodes"></param>
        public void ProcessJobCodeExceptions(Models.RetailEmployee retailEmployee, Models.HumanResourceMapping humanResourceMapping, Models.ExceptionJobCodes exceptionJobCodes)
        {
            if (exceptionJobCodes.User.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsUser = true;
            }
            if (exceptionJobCodes.Employment.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsEmployment = true;
            }
            if (exceptionJobCodes.Worker.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsWorker = true;
            }
            if (exceptionJobCodes.RetailStaff.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsRetailStaff = true;
            }
            if (exceptionJobCodes.PositionWorkerAssignment.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsPositionWorkerAssignment = true;
            }
            if (exceptionJobCodes.Position.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsPosition = true;
            }
            if (exceptionJobCodes.PersonUser.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsPersonUser = true;
            }
            if (exceptionJobCodes.Employee.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsEmployee = true;
            }
            if (exceptionJobCodes.CustomerAffliation.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsCustomerAffliation = true;
            }
            if (exceptionJobCodes.Customer.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsCustomer = true;
            }
            if (exceptionJobCodes.CommissionGroup.Contains(retailEmployee.Employment.JobCode))
            {
                humanResourceMapping.IsCommissionGroup = true;
            }
            // Is Permission exceptions found for the jobcode? update the permission Group to respective jobCodes
            if (exceptionJobCodes.Permissions.Any(x => x.JobCodes.Contains(retailEmployee.Employment.JobCode)))
            {
                humanResourceMapping.PermissionGroup = exceptionJobCodes.Permissions.Where(x => x.JobCodes.Contains(retailEmployee.Employment.JobCode)).FirstOrDefault().Group;
            }
        }

        /// <summary>
        /// Check if location is in NorthAmerica
        /// </summary>
        /// <param name="retailEmployee"></param>
        /// <returns></returns>
        public bool IsNorthAmerican(Models.RetailEmployee retailEmployee, Models.AppSettings appSettings)
        {
            return appSettings.NorthAmericaLegalEntities.Contains(retailEmployee.Employment.CompanyCode);
        }

        /// <summary>
        /// Check if location is in Europe
        /// </summary>
        /// <param name="retailEmployee"></param>
        /// <returns></returns>
        public bool IsEuropean(Models.RetailEmployee retailEmployee, Models.AppSettings appSettings)
        {
            return appSettings.EuropeLegalEntities.Contains(retailEmployee.Employment.CompanyCode);
        }
    }
}