using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebApi.Entities;
using Novell.Directory.Ldap;
using System;

namespace WebApi.Helpers
{
    public class DataContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sql server database
            options.UseMySql("server=mysql; port=3306; Database=adminprofile; Uid=root; Pwd=root");
        }

        public LdapConnection connectLDAP()
        {
            string ldapHost = "52.54.55.167";
            int ldapPort = 389;
            string loginDN = "cn=admin,dc=pinart,dc=com";
            string password = "admin";

            try
            {

                LdapConnection conn = new LdapConnection();
                Console.WriteLine("Connecting to " + ldapHost);
                conn.Connect(ldapHost, ldapPort);
                conn.Bind(loginDN, password);

                return conn;

            }
            catch (LdapException e)
            {
                Console.WriteLine("LDAP Error :" + e.LdapErrorMessage);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Normal Error :" + e.Message);
                return null;
            }
        }

        public void getAllLDAP(LdapConnection conn, string searchBase, string searchFilter)
        {
            string[] requiredAttributes = { "cn", "sn", "uid" };
            LdapSearchResults lsc = conn.Search(searchBase,
                                LdapConnection.SCOPE_SUB,
                                searchFilter,
                                requiredAttributes,
                                false);
            while (lsc.hasMore())
            {
                LdapEntry nextEntry = null;
                try
                {
                    nextEntry = lsc.next();

                }
                catch (LdapException e)
                {
                    Console.WriteLine("Error : " + e.LdapErrorMessage);
                    continue;
                }
                Console.WriteLine("\n" + nextEntry.DN);
                LdapAttributeSet attributeSet = nextEntry.getAttributeSet();
                System.Collections.IEnumerator ienum = attributeSet.GetEnumerator();
                while (ienum.MoveNext())
                {
                    LdapAttribute attribute = (LdapAttribute)ienum.Current;
                    string attributeName = attribute.Name;
                    string attributeVal = attribute.StringValue;
                    Console.WriteLine("\t" + attributeName + "\tvalue  = \t" + attributeVal);
                }
            }

        }

        public void createLDAP(LdapConnection conn, string containerName, string firstName, string secondName, string email, string username, string password)
        {
            LdapAttributeSet attributeSet = new LdapAttributeSet();
            attributeSet.Add(new LdapAttribute("objectclass", "inetOrgPerson"));
            attributeSet.Add(new LdapAttribute("cn", firstName + " " + secondName));
            attributeSet.Add(new LdapAttribute("givenname", firstName));
            attributeSet.Add(new LdapAttribute("sn", secondName));
            attributeSet.Add(new LdapAttribute("userpassword", password));
            // DN of the entry to be added
            string dn = "cn=" + username + "," + containerName;
            LdapEntry newEntry = new LdapEntry(dn, attributeSet);
            //Add the entry to the directory
            conn.Add(newEntry);
        }

        public Boolean auth(LdapConnection conn, string objectDN, string password)
        {

            LdapAttribute attr = new LdapAttribute("userPassword", password);
            bool correct = conn.Compare(objectDN, attr);

            return correct;
        }

        public void close(LdapConnection conn)
        {
            conn.Disconnect();
        }

        public DbSet<Auth> Auths { get; set; }
    }
}