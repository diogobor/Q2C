using Q2C.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Q2C.Model
{
    public class User
    {
        public int Id { get; set; }
        public string RegistrationDateStr { get; set; }
        public DateTime RegistrationDate { get => Util.Util.ConvertStrToDate(RegistrationDateStr); }
        public string Name { get; set; }
        public UserCategory Category { get; set; }
        public string _category
        {
            get => GetCategoryStr(Category);
        }

        private string GetCategoryStr(UserCategory category)
        {
            string categoryStr = "Udefined";
            switch (category)
            {
                case UserCategory.Administrator:
                    categoryStr = "Administrator";
                    break;
                case UserCategory.MasterUsrSampleMachine:
                    categoryStr = "Master User Sample & Machine";
                    break;
                case UserCategory.MasterUsrSample:
                    categoryStr = "Master User Sample";
                    break;
                case UserCategory.SuperUsrSampleMachine:
                    categoryStr = "Super User Sample & Machine";
                    break;
                case UserCategory.SuperUsrMachine:
                    categoryStr = "Super User Machine";
                    break;
                case UserCategory.SuperUsrSample:
                    categoryStr = "Super User Sample";
                    break;
                case UserCategory.UserSample:
                    categoryStr = "User Sample";
                    break;
                case UserCategory.User:
                    categoryStr = "User";
                    break;
            }
            return categoryStr;
        }
        public static UserCategory GetCategory(string category)
        {
            if (String.IsNullOrEmpty(category))
                return UserCategory.Undefined;

            switch (category)
            {
                case "Administrator":
                    return UserCategory.Administrator;
                case "Master User Sample & Machine":
                    return UserCategory.MasterUsrSampleMachine;
                case "Master User Sample":
                    return UserCategory.MasterUsrSample;
                case "Super User Sample & Machine":
                    return UserCategory.SuperUsrSampleMachine;
                case "Super User Machine":
                    return UserCategory.SuperUsrMachine;
                case "Super User Sample":
                    return UserCategory.SuperUsrSample;
                case "User Sample":
                    return UserCategory.UserSample;
                case "User":
                    return UserCategory.User;
                default:
                    return UserCategory.Undefined;
            }
        }
        //hash email
        public string Email { get; set; }

        public Management.InfoStatus InfoStatus { get; set; }
        public string _infoStatus { get => Management.GetInfoStatus(InfoStatus); }

        public User(int id, string registrationDateStr, string name, UserCategory category, string email, Management.InfoStatus infoStatus)
        {
            Id = id;
            RegistrationDateStr = registrationDateStr;
            Name = name;
            Category = category;
            Email = email;
            InfoStatus = infoStatus;
        }
    }

    public enum UserCategory
    {
        Administrator,//Add/edit/remove databases, users and machines; MasterUsrSampleMachine function
        MasterUsrSampleMachine,//Add/edit/remove samples and runs and put samples in the queue
        MasterUsrSample,//Add/edit/remove samples and put them in the queue
        SuperUsrSampleMachine,//Add/edit/remove samples and runs
        SuperUsrMachine,//Add/edit/remove runs
        SuperUsrSample,//Add/edit/remove samples and view runs
        UserSample,//Add/edit/remove samples
        User,//Only visualize waiting list
        Undefined
    }
}
