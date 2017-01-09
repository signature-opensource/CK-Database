//using CK.Core;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CK.Setup.Mixin
//{
//    interface IMixin
//    {
//    }

//    interface IMixinExtender<T,TMix> : IAmbientContract where T : IMixin
//    {
//        T Create();
//    }

//    /// <summary>
//    /// Marker interface for root Poco.
//    /// </summary>
//    interface IPoco { }

//    /// <summary>
//    /// Root Poco sample.
//    /// </summary>
//    interface IGoogleInfo : IPoco
//    {
//        byte[] RefreshToken { get; set; }

//        DateTime LastWriteRefreshToken { get; set; }
//    }

//    /// <summary>
//    /// Mixin sample.
//    /// </summary>
//    interface IGoogleInfoWithScope : IGoogleInfo
//    {
//        int ScopeSetId { get; set; }
//    }

//    /// <summary>
//    /// Another mixin sample.
//    /// </summary>
//    interface IGoogleInfoWithEMail : IGoogleInfo
//    {
//        string EMail { get; set; }
//        bool EMailVerified { get; set; }
//    }

//    //interface IPocoFactory
//    //{
//    //    /// <summary>
//    //    /// Creates an instance of a type that implements all interfaces
//    //    /// that exist on a root IPoco.
//    //    /// </summary>
//    //    /// <typeparam name="T"></typeparam>
//    //    /// <returns></returns>
//    //    T Create<T>() where T : IPoco;
//    //}

//    // Can't be a static class because of Context...
//    // Poco must be bound to a context :(
//    // Their factory must be the StObjContext.
//    //static class Poco
//    //{
//    //    public static  IPocoFactory Factory { get; }
//    //}

//    /// <summary>
//    /// Typed factory is better. Will be naturally obtained
//    /// by injection.
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    interface IPocoFactory<T> where T : IPoco
//    {
//        /// <summary>
//        /// Creates an instance of a type that implements all interfaces
//        /// that exist in the context on the T IPoco.
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <returns></returns>
//        T Create();
//    }
//    /// We need one concrete PocoFactory per root Poco (as well as one unique Type that 
//    /// implement all the interfaces).
//
//    // Poco reader? Poco processor?
//    // Chain of resposibility for an action...
//    // For reading, this makes sense: it is an initialization from a "data".
//    // Simple data: IDataRow.
//    // Each "reader" handles its columns/fields.
//    // Each "reader" can participate to the columns selection (build the select clause).
//    // This could be easy when/if there is only one reader: the full one.
//    // If it's the case, it means that a Poco is 
//    //  - bound to a "single host" (one source like a table or a view).
//    //  - we always want to read all fields of a Poco.
//    //  
//    // Idea 1: IPocoRowReader<T>, IPocoRowReader<T1, T2, etc.> 
//    // A fake generic marker interface IAll<T> could designate the final unified type and 
//    // IPocoRowReader<IAll<T>> read all existing data...
//    // 
//    // Idea 2: Or reading a Poco means reading columns from the database. These Poco are bound
//    // to a table/view. However they should be reusable (typically accross views).
//    // If they are bound to something, it to their original columns. This binding would
//    // be the access to the "meta model" from the basic "instance/row model". The actual 
//    // type of these Poco would indeed participate to the meta model.
//    //
//    // Idea 1 is the Mapper way.
//    // Idea 2 is more the ORM way.
//    //
//    /// <summary>
//    /// Root Poco sample.
//    /// </summary>
//    interface IGoogleInfoColumns : IPocoOf<UserGoogleTable>
//    {
//        byte[] RefreshToken { get; set; }

//        DateTime LastWriteRefreshToken { get; set; }
//    }
//    // Using a Poco like this make the "extension table" concept more sensible.
//    // Any package that adds columns to a table can define the Poco interface that extends 
//    // the one of the base table.
//    // One way of doing this is to consider that a SqlTable can define a 
//    // conventionnally named IColumns nested Poco.
//    // An "extension package" defines also a nested IColumns interface that extends
//    // the "base" SqlTable.IColumns interface.
//    // This IColumns definition can be specially processed (it is a "special Poco"). Its 
//    // properties may be decorated by attributes that define the schema of the table.
//    //
//    // IDbPoco (better name than IPoco?) are to instances what IAmbientContract are to services.
//    // => they both provide unified types (to the most specialized ones) while enabling the developer
//    //    to interact with the system at any (higher) abstraction level.
//    //
//    // Whatever... it seems that the IPoco & IPocoFactory is simple enough and actually indepedent
//    // of the different use one can make with them.
//    // CK.Reflection already has code to emit stupid auto properties (with backing fields)...
//    // So?
//}
