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

namespace OsmSharp.IO.API
{
	public class Client
	{
		/// <summary>
		/// The OSM base address
		/// </summary>
		/// <example>
		/// "https://master.apis.dev.openstreetmap.org/api/0.6/"
		/// "https://www.openstreetmap.org/api/0.6/"
		/// </example>
		protected readonly string BaseAddress;

		protected string OsmMaxPrecision = ".#######";

		public Client(string baseAddress)
		{
			BaseAddress = baseAddress;
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
				throw new Exception("Query can only specify userID OR userName, not both.");
			if (openOnly && closedOnly)
				throw new Exception("Query can only specify openOnly OR closedOnly, not both.");
			if (!minClosedDate.HasValue && maxOpenedDate.HasValue)
				throw new Exception("Query must specify minClosedDate if maxOpenedDate is specified.");

			var query = HttpUtility.ParseQueryString(string.Empty);
			if (bounds != null) query["bbox"] = ToString(bounds);
			if (userId.HasValue) query["user"] = userId.ToString();
			if (userName != null) query["display_name"] = userName;
			if (minClosedDate.HasValue) query["time"] = minClosedDate.ToString();
			if (maxOpenedDate.HasValue) query.Add("time", maxOpenedDate.ToString());
			if (openOnly) query["open"] = "true";
			if (closedOnly) query["closed"] = "true";
			if (ids != null)
				foreach (var id in ids)
					query.Add("changesets", id.ToString());

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
				var typed = new TypedStream();
				typed.FileName = content.Headers.ContentDisposition.FileName.Trim('"');
				typed.ContentType = content.Headers.ContentType;
				typed.Stream = await content.ReadAsStreamAsync();
				return typed;
			}
		}
		#endregion

		protected async Task<IEnumerable<T>> GetOfType<T>(string address, Action<HttpClient> auth = null) where T : class
		{
			var content = await Get(address, auth);
			var streamSource = new XmlOsmStreamSource(await content.ReadAsStreamAsync());
			var elements = streamSource.OfType<T>();
			return elements;
		}

		protected async Task<T> Get<T>(string address, Action<HttpClient> auth = null) where T : class
		{
			var content = await Get(address, auth);
			var stream = await content.ReadAsStreamAsync();
			var serializer = new XmlSerializer(typeof(T));
			var element = serializer.Deserialize(stream) as T;
			return element;
		}

		protected async Task<HttpContent> Get(string address, Action<HttpClient> auth = null)
		{
			var client = new HttpClient();
			if (auth != null) auth(client);
			var response = await client.GetAsync(address);
			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Request failed: {response.StatusCode}-{response.ReasonPhrase} {errorContent}");
			}

			return response.Content;
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

		// For GetTraceDetails() and GetTraceData(), which may be authenticated or not.
		protected virtual void AddAuthentication(HttpClient client, string url, string method = "GET") { }
	}
}

