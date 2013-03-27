using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Web.Services.Description;
using System.Net;


namespace Sitecore.SharedSource.Dataset
{
    /// <summary>
    /// Credit: http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/39138d08-aa08-4c0c-9a58-0eb81a672f54
    /// </summary>
    public class WebServiceUtil
    {
        private static object _syncRoot = new object();
        private static Dictionary<string, object> _serviceAssemblies = new Dictionary<string, object>();

        public static object GetWebServiceClientProxyClass(string webServiceAsmxUrl, string serviceClassName)
        { 
            return GetWebServiceClientProxyClass(webServiceAsmxUrl, serviceClassName, null);
        }

        public static object GetWebServiceClientProxyClass(string webServiceAsmxUrl, string serviceClassName, ICredentials credentials)
        {
            Assembly assembly = null; 
            object proxyClass = null;

            try
            {
                assembly = (Assembly)GetWebServiceClientProxyAssembly(webServiceAsmxUrl, credentials);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to dynamically create web service proxy assembly. [" + webServiceAsmxUrl + "]", ex);
            }


            try
            {
                proxyClass = assembly.CreateInstance(serviceClassName);
            }
            catch (Exception ex)
            {
                string validtypes = string.Empty;
                if (assembly != null) 
                    validtypes = string.Join(@", \n", assembly.GetTypes().Select(t => t.Name).ToArray());
                throw new Exception(string.Format("Failed to instantiate service proxy. [{0}][{1}]", webServiceAsmxUrl, serviceClassName)  + "\n\n" + validtypes, ex);
            }

            return proxyClass;
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, Unrestricted = true)]
        private static object GetWebServiceClientProxyAssembly(string webServiceAsmxUrl, ICredentials credentials)
        {
            object result = null;
            lock (_syncRoot)
            {
                if (_serviceAssemblies.ContainsKey(webServiceAsmxUrl))
                    result = _serviceAssemblies[webServiceAsmxUrl];
                else
                {
                    System.Net.WebClient client = new System.Net.WebClient();
                    if (credentials != null)
                        client.Credentials = credentials;

                    // Connect To the web service
                    System.IO.Stream stream = client.OpenRead(webServiceAsmxUrl + "?wsdl");

                    // Now read the WSDL file describing a service.
                    ServiceDescription description = ServiceDescription.Read(stream);

                    ///// LOAD THE DOM /////////
                    // Initialize a service description importer.
                    ServiceDescriptionImporter importer = new ServiceDescriptionImporter();

                    importer.ProtocolName = "Soap12"; // Use SOAP 1.2.

                    importer.AddServiceDescription(description, null, null);

                    // Generate a proxy client.
                    importer.Style = ServiceDescriptionImportStyle.Client;

                    // Generate properties to represent primitive values.
                    importer.CodeGenerationOptions = System.Xml.Serialization.CodeGenerationOptions.GenerateProperties;

                    // Initialize a Code-DOM tree into which we will import the service.
                    CodeNamespace nmspace = new CodeNamespace();

                    CodeCompileUnit unit1 = new CodeCompileUnit();

                    unit1.Namespaces.Add(nmspace);

                    // Import the service into the Code-DOM tree. This creates proxy code that uses the service.
                    ServiceDescriptionImportWarnings warning = importer.Import(nmspace, unit1);

                    if (warning == 0) // If zero then we are good to go
                    {

                        // Generate the proxy code
                        CodeDomProvider provider1 = CodeDomProvider.CreateProvider("CSharp");

                        // Compile the assembly proxy with the appropriate references
                        string[] assemblyReferences = new string[5] { "System.dll", "System.Web.Services.dll", "System.Web.dll", "System.Xml.dll", "System.Data.dll" };
                        CompilerParameters parms = new CompilerParameters(assemblyReferences);
                        CompilerResults results = provider1.CompileAssemblyFromDom(parms, unit1);

                        // Check For Errors
                        if (results.Errors.Count > 0)
                        {
                            foreach (CompilerError oops in results.Errors)
                            {
                                System.Diagnostics.Debug.WriteLine("========Compiler error============");
                                System.Diagnostics.Debug.WriteLine(oops.ErrorText);
                            }

                            throw new System.Exception("Compile Error Occured calling webservice. Check Debug ouput window.");
                        }

                        result = results.CompiledAssembly;
                        _serviceAssemblies.Add(webServiceAsmxUrl, result);
                    }
                }
            }
            return result;
        }
    }
}