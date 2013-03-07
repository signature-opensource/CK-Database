<SetupPackage FullName="Test.WithModel" Versions="{ 1.0.0 }" >
  
  <Requirements Requires="Test, Test.DependentPackage" />

  <Model>
    <!-- 
         It is not necessary to specify Requires="Model.Test.WithModelAnother": by default, 
         PackageModel.AutomaticModelRequirement is true: Models of the packages required by
         our Pachage are automatically required by the Model (if they exist).
    -->
    <Requirements RequiredBy="Test.sOneStoredProcedure" />
  </Model>
  
  <Content>
    <!-- 
          As long as no conflict occurs, the relationship between a Pakage and its Items 
          can be created by the Package and/or the Item.
    -->
    <Add FullName="Test.fTest" />

    <Add FullName="Test.sOneStoredProcedureA" />
    <Add FullName="Test.sStoredProcedureRequires" />

  </Content>
</SetupPackage>
