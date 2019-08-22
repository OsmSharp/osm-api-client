# OsmApiClient

This is a simple C# client to allow using [OSM API](https://wiki.openstreetmap.org/wiki/API_v0.6) easily.

To work with this project, you will need VisualStudio or VS Code.
Pull requests are welcome.

# Supported Opperations:
- [x] GET /api/versions
- [x] GET /api/capabilities
- [x] GET /api/0.6/map
- [x] GET /api/0.6/permissions
### Change Sets
- [x] PUT /api/0.6/changeset/create
- [x] GET /api/0.6/changeset/#id?include_discussion=true
- [x] PUT /api/0.6/changeset/#id
- [x] PUT /api/0.6/changeset/#id/close
- [x] GET /api/0.6/changeset/#id/download
- [x] GET /api/0.6/changesets
- [x] POST /api/0.6/changeset/#id/upload
### Change Set Discussion
- [x] POST /api/0.6/changeset/#id/comment
- [x] POST /api/0.6/changeset/#id/subscribe
- [x] POST /api/0.6/changeset/#id/unsubscribe
### Elements
- [x] PUT /api/0.6/[node|way|relation]/create
- [x] GET /api/0.6/[node|way|relation]/#id
- [x] PUT /api/0.6/[node|way|relation]/#id
- [x] DELETE /api/0.6/[node|way|relation]/#id
- [x] GET /api/0.6/[node|way|relation]/#id/history
- [x] GET /api/0.6/[node|way|relation]/#id/#version
- [x] GET /api/0.6/[nodes|ways|relations]?#parameters
- [x] GET /api/0.6/[node|way|relation]/#id/relations
- [x] GET /api/0.6/node/#id/ways
- [x] GET /api/0.6/[way|relation]/#id/full
### Gpx Files
- [x] GET /api/0.6/trackpoints?bbox=left,bottom,right,top&page=pageNumber
- [x] GET /api/0.6/user/gpx_files
- [x] GET /api/0.6/gpx/#id/details
- [x] GET /api/0.6/gpx/#id/data
- [x] POST /api/0.6/gpx/create
- [x] PUT /api/0.6/gpx/#id
- [x] DELETE /api/0.6/gpx/#id
### User Info
- [x] GET /api/0.6/user/#id
- [x] GET /api/0.6/users?users=#id1,#id2,...,#idn
- [x] GET /api/0.6/user/details
- [x] GET /api/0.6/user/preferences
- [x] PUT /api/0.6/user/preferences
- [x] GET /api/0.6/user/preferences/#key
- [x] PUT /api/0.6/user/preferences/#key
- [x] DELETE /api/0.6/user/preferences/#key
### Notes
- [x] GET /api/0.6/notes?#parameters
- [x] GET /api/0.6/notes/#id
- [x] POST /api/0.6/notes
- [x] POST /api/0.6/notes/#id/comment
- [x] POST /api/0.6/notes/#id/close
- [x] POST /api/0.6/notes/#id/reopen
- [x] GET /api/0.6/notes/search?#parameters
- [x] GET /api/0.6/notes/feed?bbox=left,bottom,right,top