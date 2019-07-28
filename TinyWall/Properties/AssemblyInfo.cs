using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

// Allgemeine Informationen über eine Assembly werden über die folgenden 
// Attribute gesteuert. Ändern Sie diese Attributwerte, um die Informationen zu ändern,
// die mit einer Assembly verknüpft sind.
[assembly: AssemblyTitle("TinyWall")]
[assembly: AssemblyDescription("An application to control and secure the built-in firewall of Windows.")]
[assembly: AssemblyCompany("Károly Pados")]
[assembly: AssemblyProduct("TinyWall")]
[assembly: AssemblyCopyright("Copyright © 2011 Károly Pados")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Durch Festlegen von ComVisible auf "false" werden die Typen in dieser Assembly unsichtbar 
// für COM-Komponenten. Wenn Sie auf einen Typ in dieser Assembly von 
// COM zugreifen müssen, legen Sie das ComVisible-Attribut für diesen Typ auf "true" fest.
[assembly: ComVisible(false)]

// Die folgende GUID bestimmt die ID der Typbibliothek, wenn dieses Projekt für COM verfügbar gemacht wird
[assembly: Guid("5f4d478e-347c-4b8a-8989-4e942b4e79af")]

// Versionsinformationen für eine Assembly bestehen aus den folgenden vier Werten:
//
//      Hauptversion
//      Nebenversion 
//      Buildnummer
//      Revision
//
// Sie können alle Werte angeben oder die standardmäßigen Build- und Revisionsnummern 
// übernehmen, indem Sie "*" eingeben:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.1.11")]                  // used by the CLR
[assembly: AssemblyFileVersion("2.1.11")]              // full assembly version
[assembly: AssemblyInformationalVersion("2.1.11")]     // informal version for customers
[assembly: NeutralResourcesLanguage("en")]

[assembly: CLSCompliant(true)]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
[assembly: System.Diagnostics.Debuggable(false, false)]
#endif
