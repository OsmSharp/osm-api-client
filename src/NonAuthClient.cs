﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Streams;
using OsmSharp.Streams.Complete;
using OsmSharp.Complete;
using System.Xml.Serialization;
using OsmSharp.Changesets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Microsoft.Extensions.Logging;

namespace OsmSharp.IO.API
{
    public class NonAuthClient
    {
        /// <summary>
        /// The OSM base address
        /// </summary>
        /// <example>
        /// "https://master.apis.dev.openstreetmap.org/api/0.6/"
        /// "https://www.openstreetmap.org/api/0.6/"
        /// </example>
        protected readonly string BaseAddress;

        // Prevent scientific notation in a url.
        protected string OsmMaxPrecision = "0.########";

        private readonly HttpClient _httpClient;
        protected readonly ILogger _logger;

        public NonAuthClient(string baseAddress, 
            HttpClient httpClient,
            ILogger logger)
        {
            BaseAddress = baseAddress;
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Available API versions
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Available_API_versions:_GET_.2Fapi.2Fversions">
        /// GET /api/versions</see>.
        /// </summary>
        public async Task<double?> GetVersions()
        {
            var osm = await Get<Osm>(BaseAddress + "versions");
            return osm.Api.Version.Maximum;
        }

        /// <summary>
        /// API Capabilities
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Capabilities:_GET_.2Fapi.2Fcapabilities">
        /// GET /api/capabilities</see>.
        /// </summary>
        public async Task<Osm> GetCapabilities()
        {
            return await Get<Osm>(BaseAddress + "0.6/capabilities");
        }

        /// <summary>
        /// Retrieving map data by bounding box
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Retrieving_map_data_by_bounding_box:_GET_.2Fapi.2F0.6.2Fmap">
        /// GET /api/0.6/map</see>.
        /// </summary>
        public async Task<Osm> GetMap(Bounds bounds)
        {
            Validate.BoundLimits(bounds);
            var address = BaseAddress + $"0.6/map?bbox={ToString(bounds)}";

            return await Get<Osm>(address);
        }

        /// <summary>
        /// Details of a user
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Details_of_a_user">
        /// GET /api/0.6/user/#id</see>.
        /// </summary>
        public async Task<User> GetUser(long id)
        {
            var address = BaseAddress + $"0.6/user/{id}";
            var osm = await Get<Osm>(address);
            return osm.User;
        }

        /// <summary>
        /// Details of multiple users
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Details_of_multiple_users">
        /// GET /api/0.6/users?users=#id1,#id2,...,#idn</see>.
        /// </summary>
        public async Task<User[]> GetUsers(params long[] ids)
        {
            var address = BaseAddress + $"0.6/users?users={string.Join(",", ids)}";
            var osm = await Get<Osm>(address);
            return osm.Users;
        }

        #region Elements
        public Task<CompleteWay> GetCompleteWay(long id)
        {
            return GetCompleteElement<CompleteWay>(id);
        }

        public Task<CompleteRelation> GetCompleteRelation(long id)
        {
            return GetCompleteElement<CompleteRelation>(id);
        }

        /// <summary>
        /// Element Full
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Full:_GET_.2Fapi.2F0.6.2F.5Bway.7Crelation.5D.2F.23id.2Ffull">
        /// GET /api/0.6/[way|relation]/#id/full</see>.
        /// </summary>
        private async Task<TCompleteOsmGeo> GetCompleteElement<TCompleteOsmGeo>(long id) where TCompleteOsmGeo : ICompleteOsmGeo, new()
        {
            var type = new TCompleteOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}/full";
            var content = await Get(address);
            var stream = await content.ReadAsStreamAsync();
            var streamSource = new XmlOsmStreamSource(stream);
            var completeSource = new OsmSimpleCompleteStreamSource(streamSource);
            var element = completeSource.OfType<TCompleteOsmGeo>().FirstOrDefault();
            return element;
        }

        public async Task<Node> GetNode(long id)
        {
            return await GetElement<Node>(id);
        }

        public async Task<Way> GetWay(long id)
        {
            return await GetElement<Way>(id);
        }

        public async Task<Relation> GetRelation(long id)
        {
            return await GetElement<Relation>(id);
        }

        /// <summary>
        /// Element Read
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id">
        /// GET /api/0.6/[node|way|relation]/#id</see>.
        /// </summary>
        private async Task<TOsmGeo> GetElement<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}";
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.FirstOrDefault();
        }

        public async Task<Node[]> GetNodeHistory(long id)
        {
            return await GetElementHistory<Node>(id);
        }

        public async Task<Way[]> GetWayHistory(long id)
        {
            return await GetElementHistory<Way>(id);
        }

        public async Task<Relation[]> GetRelationHistory(long id)
        {
            return await GetElementHistory<Relation>(id);
        }

        /// <summary>
        /// Element History
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#History:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Fhistory">
        /// GET /api/0.6/[node|way|relation]/#id/history</see>.
        /// </summary>
        private async Task<TOsmGeo[]> GetElementHistory<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}/history";
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.ToArray();
        }

        public async Task<Node> GetNodeVersion(long id, int version)
        {
            return await GetElementVersion<Node>(id, version);
        }

        public async Task<Way> GetWayVersion(long id, int version)
        {
            return await GetElementVersion<Way>(id, version);
        }

        public async Task<Relation> GetRelationVersion(long id, int version)
        {
            return await GetElementVersion<Relation>(id, version);
        }

        /// <summary>
        /// Element Version
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Version:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2F.23version">
        /// GET /api/0.6/[node|way|relation]/#id/#version</see>.
        /// </summary>
        private async Task<TOsmGeo> GetElementVersion<TOsmGeo>(long id, int version) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}/{version}";
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.FirstOrDefault();
        }

        public async Task<Node[]> GetNodes(params long[] ids)
        {
            return await GetElements<Node>(ids);
        }

        public async Task<Way[]> GetWays(params long[] ids)
        {
            return await GetElements<Way>(ids);
        }

        public async Task<Relation[]> GetRelations(params long[] ids)
        {
            return await GetElements<Relation>(ids);
        }

        private async Task<TOsmGeo[]> GetElements<TOsmGeo>(params long[] ids) where TOsmGeo : OsmGeo, new()
        {
            var idVersions = ids.Select(id => new KeyValuePair<long, int?>(id, null));
            return await GetElements<TOsmGeo>(idVersions);
        }

        public async Task<Node[]> GetNodes(IEnumerable<KeyValuePair<long, int?>> idVersions)
        {
            return await GetElements<Node>(idVersions);
        }

        public async Task<Way[]> GetWays(IEnumerable<KeyValuePair<long, int?>> idVersions)
        {
            return await GetElements<Way>(idVersions);
        }

        public async Task<Relation[]> GetRelations(IEnumerable<KeyValuePair<long, int?>> idVersions)
        {
            return await GetElements<Relation>(idVersions);
        }

        /// <summary>
        /// Elements Multifetch
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Multi_fetch:_GET_.2Fapi.2F0.6.2F.5Bnodes.7Cways.7Crelations.5D.3F.23parameters">
        /// GET /api/0.6/[nodes|ways|relations]?#parameters</see>.
        /// </summary>
        private async Task<TOsmGeo[]> GetElements<TOsmGeo>(IEnumerable<KeyValuePair<long, int?>> idVersions) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            // For exmple: "12,13,14v1,15v1"
            var parameters = string.Join(",", idVersions.Select(e => e.Value.HasValue ? $"{e.Key}v{e.Value}" : e.Key.ToString()));
            var address = BaseAddress + $"0.6/{type}s?{type}s={parameters}";
            var elements = await GetOfType<TOsmGeo>(address);
            return elements.ToArray();
        }

        public async Task<Relation[]> GetNodeRelations(long id)
        {
            return await GetElementRelations<Node>(id);
        }

        public async Task<Relation[]> GetWayRelations(long id)
        {
            return await GetElementRelations<Way>(id);
        }

        public async Task<Relation[]> GetRelationRelations(long id)
        {
            return await GetElementRelations<Relation>(id);
        }

        /// <summary>
        /// Element Relations
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Relations_for_element:_GET_.2Fapi.2F0.6.2F.5Bnode.7Cway.7Crelation.5D.2F.23id.2Frelations">
        /// GET /api/0.6/[node|way|relation]/#id/relations</see>.
        /// </summary>
        private async Task<Relation[]> GetElementRelations<TOsmGeo>(long id) where TOsmGeo : OsmGeo, new()
        {
            var type = new TOsmGeo().Type.ToString().ToLower();
            var address = BaseAddress + $"0.6/{type}/{id}/relations";
            var elements = await GetOfType<Relation>(address);
            return elements.ToArray();
        }

        /// <summary>
        /// Node Ways
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Ways_for_node:_GET_.2Fapi.2F0.6.2Fnode.2F.23id.2Fways">
        /// GET /api/0.6/node/#id/ways</see>.
        /// </summary>
        public async Task<Way[]> GetNodeWays(long id)
        {
            var address = BaseAddress + $"0.6/node/{id}/ways";
            var elements = await GetOfType<Way>(address);
            return elements.ToArray();
        }
        #endregion

        #region Changesets
        /// <summary>
        /// Changeset Read
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2Fchangeset.2F.23id.3Finclude_discussion.3Dtrue">
        /// GET /api/0.6/changeset/#id?include_discussion=true</see>.
        /// </summary>
        public async Task<Changeset> GetChangeset(long changesetId, bool includeDiscussion = false)
        {
            var address = BaseAddress + $"0.6/changeset/{changesetId}";
            if (includeDiscussion)
            {
                address += "?include_discussion=true";
            }
            var osm = await Get<Osm>(address);
            return osm.Changesets[0];
        }

        /// <summary>
        /// Changeset Query
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Query:_GET_.2Fapi.2F0.6.2Fchangesets">
        /// GET /api/0.6/changesets</see>
        /// </summary>
        public async Task<Changeset[]> QueryChangesets(Bounds bounds, long? userId, string userName,
            DateTime? minClosedDate, DateTime? maxOpenedDate, bool openOnly, bool closedOnly,
            long[] ids)
        {
            if (userId.HasValue && userName != null)
                throw new ArgumentException("Query can only specify userID OR userName, not both.");
            if (openOnly && closedOnly)
                throw new ArgumentException("Query can only specify openOnly OR closedOnly, not both.");
            if (!minClosedDate.HasValue && maxOpenedDate.HasValue)
                throw new ArgumentException("Query must specify minClosedDate if maxOpenedDate is specified.");

            var query = HttpUtility.ParseQueryString(string.Empty);
            if (bounds != null) query["bbox"] = ToString(bounds);
            if (userId.HasValue) query["user"] = userId.ToString();
            if (userName != null) query["display_name"] = userName;
            if (minClosedDate.HasValue) query["time"] = minClosedDate.ToString();
            if (maxOpenedDate.HasValue) query.Add("time", maxOpenedDate.ToString());
            if (openOnly) query["open"] = "true";
            if (closedOnly) query["closed"] = "true";
            if (ids != null) query["changesets"] = string.Join(",", ids);

            var address = BaseAddress + "0.6/changesets?" + query.ToString();
            var osm = await Get<Osm>(address);
            return osm.Changesets;
        }

        /// <summary>
        /// Changeset Download
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Read:_GET_.2Fapi.2F0.6.2Fchangeset.2F.23id.3Finclude_discussion.3Dtrue">
        /// GET /api/0.6/changeset/#id/download</see>
        /// </summary>
        public async Task<OsmChange> GetChangesetDownload(long changesetId)
        {
            return await Get<OsmChange>(BaseAddress + $"0.6/changeset/{changesetId}/download");
        }
        #endregion

        #region Traces
        /// <summary>
        /// Get GPS Points
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Get_GPS_Points:_Get_.2Fapi.2F0.6.2Ftrackpoints.3Fbbox.3Dleft.2Cbottom.2Cright.2Ctop.26page.3DpageNumber">
        /// Get /api/0.6/trackpoints?bbox=left,bottom,right,top&page=pageNumber</see>.
        /// Retrieve the GPS track points that are inside a given bounding box (formatted in a GPX format).
        /// Warning: GPX version 1.0 is not the current version. Your tools might not support it.
        /// </summary>
        /// <returns>A stream of a GPX (version 1.0) file.</returns>
        public virtual async Task<Stream> GetTrackPoints(Bounds bounds, int pageNumber = 0)
        {
            var address = BaseAddress + $"0.6/trackpoints?bbox={ToString(bounds)}&page={pageNumber}";
            var content = await Get(address);
            var stream = await content.ReadAsStreamAsync();
            return stream;
        }

        /// <summary>
        /// Download Metadata
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Download_Metadata:_GET_.2Fapi.2F0.6.2Fgpx.2F.23id.2Fdetails">
        /// GET /api/0.6/gpx/#id/details</see>.
        /// </summary>
        public async Task<GpxFile> GetTraceDetails(int id)
        {
            var address = BaseAddress + $"0.6/gpx/{id}/details";
            var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
            return osm.GpxFiles[0];
        }

        /// <summary>
        /// Download Data
        /// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Download_Data:_GET_.2Fapi.2F0.6.2Fgpx.2F.23id.2Fdata">
        /// GET /api/0.6/gpx/#id/data</see>.
        /// This will return exactly what was uploaded, which might not be a gpx file (it could be a zip etc.)
        /// </summary>
        /// <returns>A stream of a GPX (version 1.0) file.</returns>
        public async Task<TypedStream> GetTraceData(int id)
        {
            var address = BaseAddress + $"0.6/gpx/{id}/data";
            var content = await Get(address, c => AddAuthentication(c, address));
            return await TypedStream.Create(content);
        }

        public class TypedStream
        {
            public Stream Stream;
            public string FileName;
            public System.Net.Http.Headers.MediaTypeHeaderValue ContentType;

            internal static async Task<TypedStream> Create(HttpContent content)
            {
                return new TypedStream
                {
                    FileName = content.Headers.ContentDisposition?.FileName.Trim('"'),
                    ContentType = content.Headers.ContentType,
                    Stream = await content.ReadAsStreamAsync()
                };
            }
        }
        #endregion

        #region Notes
        public async Task<Note> GetNote(long id)
        {
            var address = BaseAddress + $"0.6/notes/{id}";
            var osm = await Get<Osm>(address);
            return osm.Notes?.FirstOrDefault();
        }

        /// <param name="limit">Must be between 1 and 10,000.</param>
        /// <param name="maxClosedDays">0 means only open notes. -1 mean all (open and closed) notes.</param>
        public async Task<Note[]> GetNotes(Bounds bounds, int limit = 100, int maxClosedDays = 7)
        {
            string format = ".xml";
            var address = BaseAddress + $"0.6/notes{format}?bbox={ToString(bounds)}&limit={limit}&closed={maxClosedDays}";
            var osm = await Get<Osm>(address);
            return osm.Notes;
        }

        public async Task<Stream> GetNotesRssFeed(Bounds bounds)
        {
            var address = BaseAddress + $"0.6/notes/feed?bbox={ToString(bounds)}";
            var content = await Get(address);
            var stream = await content.ReadAsStreamAsync();
            return stream;
        }

        /// <summary>
        /// </summary>
        /// <param name="searchText">Specifies the search query. This is the only required field.</param>
        /// <param name="userId">Specifies the creator of the returned notes by the id of the user. Does not work together with the display_name parameter</param>
        /// <param name="userName">Specifies the creator of the returned notes by the display name. Does not work together with the user parameter</param>
        /// <param name="limit">Must be between 1 and 10,000. 100 is default if null.</param>
        /// <param name="maxClosedDays">0 means only open notes. -1 mean all (open and closed) notes. 7 is default if null.</param>
        /// <param name="fromDate">Specifies the beginning of a date range to search in for a note</param>
        /// <param name="toDate">Specifies the end of a date range to search in for a note</param>
        public async Task<Note[]> QueryNotes(string searchText, long? userId, string userName,
            int? limit, int? maxClosedDays, DateTime? fromDate, DateTime? toDate)
        {
            if (userId.HasValue && userName != null)
                throw new ArgumentException("Query can only specify userID OR userName, not both.");
            if (fromDate > toDate)
                throw new ArgumentException("Query [fromDate] must be before [toDate] if both are provided.");
            if (searchText == null)
                throw new ArgumentException("Query searchText is required.");

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["q"] = searchText;
            if (limit != null) query["limit"] = limit.ToString();
            if (maxClosedDays != null) query["closed"] = maxClosedDays.ToString();
            if (userName != null) query["display_name"] = userName;
            if (userId != null) query["user"] = userId.ToString();
            if (fromDate != null) query["from"] = FormatNoteDate(fromDate.Value);
            if (toDate != null) query["to"] = FormatNoteDate(toDate.Value);

            string format = ".xml";
            var address = BaseAddress + $"0.6/notes/search{format}?{query}";
            var osm = await Get<Osm>(address);
            return osm.Notes;
        }

        private static string FormatNoteDate(DateTime date)
        {
            // DateTimes in notes are 'different'.
            return date.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
        }

        public async Task<Note> CreateNote(float latitude, float longitude, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            query["lat"] = ToString(latitude);
            query["lon"] = ToString(longitude);

            var address = BaseAddress + $"0.6/notes?{query}";
            // Can be with Auth or without.
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }

        public async Task<Note> CommentNote(long noteId, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            var address = BaseAddress + $"0.6/notes/{noteId}/comment?{query}";
            // Can be with Auth or without.
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }
        #endregion

        protected async Task<IEnumerable<T>> GetOfType<T>(string address, Action<HttpRequestMessage> auth = null) where T : class
        {
            var content = await Get(address, auth);
            var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
            var elements = streamSource.OfType<T>();
            return elements;
        }

        protected string ToString(Bounds bounds)
        {
            StringBuilder x = new StringBuilder();
            x.Append(bounds.MinLongitude.Value.ToString(OsmMaxPrecision));
            x.Append(',');
            x.Append(bounds.MinLatitude.Value.ToString(OsmMaxPrecision));
            x.Append(',');
            x.Append(bounds.MaxLongitude.Value.ToString(OsmMaxPrecision));
            x.Append(',');
            x.Append(bounds.MaxLatitude.Value.ToString(OsmMaxPrecision));

            return x.ToString();
        }

        protected string ToString(float number)
        {
            return number.ToString(OsmMaxPrecision);
        }

        #region Http
        protected async Task<T> Get<T>(string address, Action<HttpRequestMessage> auth = null) where T : class
        {
            var content = await Get(address, auth);
            var stream = await content.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var element = serializer.Deserialize(stream) as T;
            return element;
        }

        protected async Task<HttpContent> Get(string address, Action<HttpRequestMessage> auth = null)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address))
            {
                _logger?.LogInformation($"GET: {address}");
                auth?.Invoke(request);
                var response = await _httpClient.SendAsync(request);
                await VerifyAndLogReponse(response);
                return response.Content;
            }
        }

        protected async Task<T> Post<T>(string address, HttpContent requestContent = null) where T : class
        {
            var responseContent = await SendAuthRequest(HttpMethod.Post, address, requestContent);
            var stream = await responseContent.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var content = serializer.Deserialize(stream) as T;
            return content;
        }

        protected async Task<T> Put<T>(string address, HttpContent requestContent = null) where T : class
        {
            var content = await SendAuthRequest(HttpMethod.Put, address, requestContent);
            var stream = await content.ReadAsStreamAsync();
            var serializer = new XmlSerializer(typeof(T));
            var element = serializer.Deserialize(stream) as T;
            return element;
        }

        /// <summary>
        /// For GetTraceDetails() and GetTraceData(), which may be authenticated or not.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="url"></param>
        /// <param name="method"></param>
        protected virtual void AddAuthentication(HttpRequestMessage message, string url, string method = "GET") { }

        protected async Task<HttpContent> SendAuthRequest(HttpMethod method, string address, HttpContent requestContent)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, address))
            {
                _logger?.LogInformation($"{method}: {address}");
                AddAuthentication(request, address, method.ToString());
                request.Content = requestContent;
                var response = await _httpClient.SendAsync(request);
                await VerifyAndLogReponse(response);
                return response.Content;
            }
        }

        protected async Task VerifyAndLogReponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                message = $"Request failed: {response.StatusCode}-{response.ReasonPhrase} {message}";
                _logger?.LogError(message);
                throw new OsmApiException(response.RequestMessage?.RequestUri, message, response.StatusCode);
            }
            else
            {
                var message = $"Request succeeded: {response.StatusCode}-{response.ReasonPhrase}";
                _logger?.LogInformation(message);
                var headers = string.Join(", ", response.Content.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}"));
                _logger?.LogDebug(headers);
            }
        }
        #endregion
    }
}
