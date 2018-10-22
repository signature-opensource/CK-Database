
[assembly: CK.Setup.IsSetupDependency()]
// Version 8.0.1--0026-develop introduces IsModelDependent and IsModelDependentSource.
// The new CK.SqlServer uses it.
[assembly: CK.Setup.RequiredSetupDependency("CK.Setup.Runner", minDependencyVersion: "8.0.1--0026-develop" )]
