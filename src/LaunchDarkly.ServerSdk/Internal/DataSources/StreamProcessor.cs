using System;
using System.Net.Http;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Internal.Stream;
using LaunchDarkly.Sdk.Server.Interfaces;
using LaunchDarkly.Sdk.Server.Internal.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using static LaunchDarkly.Sdk.Server.Interfaces.DataStoreTypes;

namespace LaunchDarkly.Sdk.Server.Internal.DataSources
{
    internal class StreamProcessor : IDataSource, IStreamProcessor
    {
        private const String PUT = "put";
        private const String PATCH = "patch";
        private const String DELETE = "delete";

        private readonly StreamManager _streamManager;
        private readonly IDataSourceUpdates _dataSourceUpdates;
        private readonly Logger _log;

        internal StreamProcessor(
            LdClientContext context,
            IDataSourceUpdates dataSourceUpdates,
            Uri baseUri,
            TimeSpan initialReconnectDelay,
            StreamManager.EventSourceCreator eventSourceCreator
            )
        {
            _log = context.Basic.Logger.SubLogger(LogNames.DataSourceSubLog);

            var streamProperties = new StreamProperties(
                new Uri(baseUri, "/all"),
                HttpMethod.Get,
                null
                );
            _streamManager = new StreamManager(this,
                streamProperties,
                context.Configuration.HttpProperties,
                initialReconnectDelay,
                eventSourceCreator,
                context.DiagnosticStore,
                _log
                );
            _dataSourceUpdates = dataSourceUpdates;
        }

        #region IDataSource

        bool IDataSource.Initialized()
        {
            return _streamManager.Initialized;
        }

        Task<bool> IDataSource.Start()
        {
            return _streamManager.Start();
        }

        #endregion

        #region IStreamProcessor

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        // This method is async because that's how the dotnet-eventsource API wants message handlers to be. There's no
        // problem with *not* doing any awaits in the method, and in fact for the SDK's purposes it's best that this
        // method behaves synchronously because that ensures that stream messages are processed in the order received.
        public async Task HandleMessage(StreamManager streamManager, string messageType, string messageData)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            switch (messageType)
            {
                case PUT:
                    _dataSourceUpdates.Init(JsonUtil.DecodeJson<PutData>(messageData).Data.ToInitData());
                    streamManager.Initialized = true;
                    break;
                case PATCH:
                    PatchData patchData = JsonUtil.DecodeJson<PatchData>(messageData);
                    string patchKey;
                    if (GetKeyFromPath(patchData.Path, DataKinds.Features, out patchKey))
                    {
                        FeatureFlag flag = patchData.Data.ToObject<FeatureFlag>();
                        _dataSourceUpdates.Upsert(DataKinds.Features, patchKey, new ItemDescriptor(flag.Version, flag));
                    }
                    else if (GetKeyFromPath(patchData.Path, DataKinds.Segments, out patchKey))
                    {
                        Segment segment = patchData.Data.ToObject<Segment>();
                        _dataSourceUpdates.Upsert(DataKinds.Segments, patchKey, new ItemDescriptor(segment.Version, segment));
                    }
                    else
                    {
                        _log.Warn("Received patch event with unknown path: {0}", patchData.Path);
                    }
                    break;
                case DELETE:
                    DeleteData deleteData = JsonUtil.DecodeJson<DeleteData>(messageData);
                    var tombstone = new ItemDescriptor(deleteData.Version, null);
                    string deleteKey;
                    if (GetKeyFromPath(deleteData.Path, DataKinds.Features, out deleteKey))
                    {
                        _dataSourceUpdates.Upsert(DataKinds.Features, deleteKey, tombstone);
                    }
                    else if (GetKeyFromPath(deleteData.Path, DataKinds.Segments, out deleteKey))
                    {
                        _dataSourceUpdates.Upsert(DataKinds.Segments, deleteKey, tombstone);
                    }
                    else
                    {
                        _log.Warn("Received delete event with unknown path: {0}", deleteData.Path);
                    }
                    break;
            }
        }

        public void HandleError(StreamManager streamManager, Exception e, bool recoverable)
        {

        }

        #endregion

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((IDisposable)_streamManager).Dispose();
            }
        }

        private static string GetDataKindPath(DataKind kind)
        {
            if (kind == DataKinds.Features)
            {
                return "/flags/";
            }
            else if (kind == DataKinds.Segments)
            {
                return "/segments/";
            }
            return null;
        }

        private static bool GetKeyFromPath(string path, DataKind kind, out string key)
        {
            if (path.StartsWith(GetDataKindPath(kind)))
            {
                key = path.Substring(GetDataKindPath(kind).Length);
                return true;
            }
            key = null;
            return false;
        }

        internal class PutData
        {
            internal AllData Data { get; private set; }

            [JsonConstructor]
            internal PutData(AllData data)
            {
                Data = data;
            }
        }

        internal class PatchData
        {
            internal string Path { get; private set; }
            internal JToken Data { get; private set; }

            [JsonConstructor]
            internal PatchData(string path, JToken data)
            {
                Path = path;
                Data = data;
            }
        }

        internal class DeleteData
        {
            internal string Path { get; private set; }
            internal int Version { get; private set; }

            [JsonConstructor]
            internal DeleteData(string path, int version)
            {
                Path = path;
                Version = version;
            }
        }
    }
}
