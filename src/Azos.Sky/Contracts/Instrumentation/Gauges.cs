/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Azos.Apps;
using Azos.Glue;
using Azos.Conf;
using Azos.Collections;
using Azos.Glue.Protocol;
using Azos.Serialization.BSON;

namespace Azos.Sky.Contracts.Instrumentation
{

    [Serializable]
    public abstract class ServiceClientHubEvent : Azos.Instrumentation.Event, Azos.Instrumentation.INetInstrument
    {
      protected ServiceClientHubEvent(string src) : base(src) { }
    }

    [Serializable]
    public abstract class ServiceClientHubErrorEvent : ServiceClientHubEvent, Azos.Instrumentation.IErrorInstrument
    {
      protected ServiceClientHubErrorEvent(string src) : base(src) { }
    }


    //todo Need to add the counter for successful calls as well, however be careful,
    //as to many details may create much instrumentation data (don't include contract+toHost)?
    //or have level of detalization setting


    [Serializable]
    [BSONSerializable("D2BDA600-7B13-4701-BC8F-C9E72A26CED8")]
    public class ServiceClientHubRetriableCallError : ServiceClientHubErrorEvent
    {
      protected ServiceClientHubRetriableCallError(string src) : base(src) { }

      public static void Happened(Type tContract, string toName)
      {
        var inst = ExecutionContext.Application.Instrumentation;
        if (inst.Enabled)
          inst.Record(new ServiceClientHubRetriableCallError("{0}::{1}".Args(tContract.FullName, toName)));
      }

      public override string Description { get { return "Service client hub retriable call failed"; } }


      protected override Azos.Instrumentation.Datum MakeAggregateInstance()
      {
        return new ServiceClientHubRetriableCallError(this.Source);
      }
    }

}
