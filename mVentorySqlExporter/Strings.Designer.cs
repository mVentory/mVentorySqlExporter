﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace mvSqlExporter {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Strings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Strings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("mvSqlExporter.Strings", typeof(Strings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Done in {0} minutes..
        /// </summary>
        internal static string msgDoneIn {
            get {
                return ResourceManager.GetString("msgDoneIn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Field configured, but is not present in the dataset: {0}
        ///Press any key to exit..
        /// </summary>
        internal static string msgMissingField {
            get {
                return ResourceManager.GetString("msgMissingField", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Saving into {0}..
        /// </summary>
        internal static string msgSavingInto {
            get {
                return ResourceManager.GetString("msgSavingInto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exporting product data from the products database. See app config for paths and settings. It may take a few mins ....
        /// </summary>
        internal static string msgWelcome {
            get {
                return ResourceManager.GetString("msgWelcome", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SKU field is not present in the list of fields: {0}
        ///Press any key to exit..
        /// </summary>
        internal static string msgWrongSKU {
            get {
                return ResourceManager.GetString("msgWrongSKU", resourceCulture);
            }
        }
    }
}
