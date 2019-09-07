﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.API;
using OsmSharp.Changesets;
using OsmSharp.Tags;
using OsmSharp.Complete;
using OsmSharp.IO.Xml;
using System.Web;
using Microsoft.Extensions.Logging;

namespace OsmSharp.IO.API
{
	public abstract class 
        AuthClient : NonAuthClient
	{
		public AuthClient(string baseAddress, HttpClient httpClient, ILogger logger) : base(baseAddress, httpClient, logger)
		{ }

		#region Users
		public async Task<Permissions> GetPermissions()
		{
			var address = BaseAddress + "0.6/permissions";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.Permissions;
		}

		public async Task<User> GetUserDetails()
		{

			var address = BaseAddress + "0.6/user/details";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.User;
		}

		public async Task<Preference[]> GetUserPreferences()
		{
			var address = BaseAddress + "0.6/user/preferences";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.Preferences.UserPreferences;
		}

		public async Task SetUserPreferences(Preferences preferences)
		{
			var address = BaseAddress + "0.6/user/preferences";
			var osm = new Osm() { Preferences = preferences };
			var content = new StringContent(osm.SerializeToXml());
			await SendAuthRequest(HttpMethod.Put, address, content);
		}

		public async Task<string> GetUserPreference(string key)
		{
			var address = BaseAddress + $"0.6/user/preferences/{key}";
			var content = await Get(address, c => AddAuthentication(c, address));
			var value = await content.ReadAsStringAsync();
			return value;
		}

		public async Task SetUserPreference(string key, string value)
		{
			var address = BaseAddress + $"0.6/user/preferences/{key}";
			var content = new StringContent(value);
			await SendAuthRequest(HttpMethod.Put, address, content);
		}

		public async Task DeleteUserPreference(string key)
		{
			var address = BaseAddress + $"0.6/user/preferences/{key}";
			await SendAuthRequest(HttpMethod.Delete, address, null);
		}
		#endregion

		#region Changesets and Element Changes
		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<long> CreateChangeset(TagsCollectionBase tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");
			var address = BaseAddress + "0.6/changeset/create";
			var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
			var content = new StringContent(changeSet.SerializeToXml());
			var resultContent = await SendAuthRequest(HttpMethod.Put, address, content);
			var id = await resultContent.ReadAsStringAsync();
			return long.Parse(id);
		}

		/// <param name="tags">Must at least contain 'comment' and 'created_by'.</param>
		public async Task<Changeset> UpdateChangeset(long changesetId, TagsCollectionBase tags)
		{
			Validate.ContainsTags(tags, "comment", "created_by");
			// TODO: Validate change meets OsmSharp.API.Capabilities?
			var address = BaseAddress + $"0.6/changeset/{changesetId}";
			var changeSet = new Osm { Changesets = new[] { new Changeset { Tags = tags } } };
			var content = new StringContent(changeSet.SerializeToXml());
			var osm = await Put<Osm>(address, content);
			return osm.Changesets[0];
		}

		/// <remarks>This automatically adds the ChangeSetId tag to each element.</remarks>
		public async Task<DiffResult> UploadChangeset(long changesetId, OsmChange osmChange)
		{
			var elements = new OsmGeo[][] { osmChange.Create, osmChange.Modify, osmChange.Delete }
				.Where(c => c != null).SelectMany(c => c);

			foreach (var osmGeo in elements)
			{
				osmGeo.ChangeSetId = changesetId;
			}

			var address = BaseAddress + $"0.6/changeset/{changesetId}/upload";
			var request = new StringContent(osmChange.SerializeToXml());

			return await Post<DiffResult>(address, request);
		}

		public async Task<long> CreateElement(long changesetId, OsmGeo osmGeo)
		{
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/create";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var response = await SendAuthRequest(HttpMethod.Put, address, content);
			var id = await response.ReadAsStringAsync();
			return long.Parse(id);
		}

		public async Task UpdateElement(long changesetId, ICompleteOsmGeo osmGeo)
		{
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					await UpdateElement(changesetId, osmGeo as OsmGeo);
					break;
				case OsmGeoType.Way:
					await UpdateElement(changesetId, ((CompleteWay)osmGeo).ToSimple());
					break;
				case OsmGeoType.Relation:
					await UpdateElement(changesetId, ((CompleteRelation)osmGeo).ToSimple());
					break;
				default:
					throw new Exception($"Invalid OSM geometry type: {osmGeo.Type}");
			}
		}

		public async Task<int> UpdateElement(long changesetId, OsmGeo osmGeo)
		{
			Validate.ElementHasAVersion(osmGeo);
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var responseContent = await SendAuthRequest(HttpMethod.Put, address, content);
			var newVersionNumber = await responseContent.ReadAsStringAsync();
			return int.Parse(newVersionNumber);
		}

		public async Task<int> DeleteElement(long changesetId, OsmGeo osmGeo)
		{
			Validate.ElementHasAVersion(osmGeo);
			var address = BaseAddress + $"0.6/{osmGeo.Type.ToString().ToLower()}/{osmGeo.Id}";
			var osmRequest = GetOsmRequest(changesetId, osmGeo);
			var content = new StringContent(osmRequest.SerializeToXml());
			var responseContent = await SendAuthRequest(HttpMethod.Delete, address, content);
			var newVersionNumber = await responseContent.ReadAsStringAsync();
			return int.Parse(newVersionNumber);
		}

		public async Task CloseChangeset(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/close";
			await SendAuthRequest(HttpMethod.Put, address, new StringContent(""));
		}

		/// <summary>
		/// Comment
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Comment:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fcomment">
		/// POST /api/0.6/changeset/#id/comment </see>
		/// </summary>
		public async Task<Changeset> AddChangesetComment(long changesetId, string text)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/comment";
			var content = new MultipartFormDataContent() { { new StringContent(text), "text" } };
			var osm = await Post<Osm>(address, content);
			return osm.Changesets[0];
		}

		/// <summary>
		/// Subscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Fsubscribe">
		/// POST /api/0.6/changeset/#id/subscribe </see>
		/// </summary>
		public async Task ChangesetSubscribe(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/subscribe";
			await SendAuthRequest(HttpMethod.Post, address, new StringContent(""));
		}

		/// <summary>
		/// Unsubscribe
		/// <see href="https://wiki.openstreetmap.org/wiki/API_v0.6#Subscribe:_POST_.2Fapi.2F0.6.2Fchangeset.2F.23id.2Funsubscribe">
		/// POST /api/0.6/changeset/#id/unsubscribe </see>
		/// </summary>
		public async Task ChangesetUnsubscribe(long changesetId)
		{
			var address = BaseAddress + $"0.6/changeset/{changesetId}/unsubscribe";
			await SendAuthRequest(HttpMethod.Post, address, new StringContent(""));
		}
		#endregion

		#region Traces
		public async Task<GpxFile[]> GetTraces()
		{
			var address = BaseAddress + "0.6/user/gpx_files";
			var osm = await Get<Osm>(address, c => AddAuthentication(c, address));
			return osm.GpxFiles ?? new GpxFile[0];
		}

		public async Task<int> CreateTrace(GpxFile gpx, Stream fileStream)
		{
			var address = BaseAddress + "0.6/gpx/create";
			var form = new MultipartFormDataContent();
			form.Add(new StringContent(gpx.Description), "\"description\"");
			form.Add(new StringContent(gpx.Visibility.ToString().ToLower()), "\"visibility\"");
			var tags = string.Join(",", gpx.Tags ?? new string[0]);
			form.Add(new StringContent(tags), "\"tags\"");
			var stream = new StreamContent(fileStream);
			var cleanName = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(gpx.Name));
			form.Add(stream, "file", cleanName);
			var content = await SendAuthRequest(HttpMethod.Post, address, form);
			var id = await content.ReadAsStringAsync();
			return int.Parse(id);
		}

		public async Task UpdateTrace(GpxFile trace)
		{
			var address = BaseAddress + $"0.6/gpx/{trace.Id}";
			var osm = new Osm { GpxFiles = new[] { trace } };
			var content = new StringContent(osm.SerializeToXml());
			await SendAuthRequest(HttpMethod.Put, address, content);
		}

		public async Task DeleteTrace(long traceId)
		{
			var address = BaseAddress + $"0.6/gpx/{traceId}";
			await SendAuthRequest(HttpMethod.Delete, address, null);
		}
        #endregion

        #region Notes
        public async Task<Note> CommentNote(long noteId, string text)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = text;
            var address = BaseAddress + $"0.6/notes/{noteId}/comment?{query}";
            // Can be with Auth or without.
            var osm = await Post<Osm>(address);
            return osm.Notes[0];
        }

        public async Task<Note> CloseNote(long noteId, string text)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["text"] = text;
			var address = BaseAddress + $"0.6/notes/{noteId}/close?{query}";
			var osm = await Post<Osm>(address);
			return osm.Notes[0];
		}

		public async Task<Note> ReOpenNote(long noteId, string text)
		{
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["text"] = text;
			var address = BaseAddress + $"0.6/notes/{noteId}/reopen?{query}";
			var osm = await Post<Osm>(address);
			return osm.Notes[0];
		}
		#endregion

		protected Osm GetOsmRequest(long changesetId, OsmGeo osmGeo)
		{
			osmGeo.ChangeSetId = changesetId;
			var osm = new Osm();
			switch (osmGeo.Type)
			{
				case OsmGeoType.Node:
					osm.Nodes = new[] { osmGeo as Node };
					break;
				case OsmGeoType.Way:
					osm.Ways = new[] { osmGeo as Way };
					break;
				case OsmGeoType.Relation:
					osm.Relations = new[] { osmGeo as Relation };
					break;
			}
			return osm;
		}
	}
}

