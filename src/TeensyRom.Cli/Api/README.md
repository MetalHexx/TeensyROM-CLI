# tr-web API client

The CLI talks to tr-web through a client generated from tr-web's own OpenAPI spec.

| File | What it is |
|------|------------|
| `TeensyRom.Api.json` | Copy of tr-web's `src/apps/api/src/TeensyRom.Api/api-spec/TeensyRom.Api.json` at the pinned version. Not tr-web's root `src/openapi-spec.json`, which is a stale copy. |
| `nswag.json` | Generator settings. |
| `Generated/TeensyRomApiClient.g.cs` | Generated, committed. Never hand-edited. |
| `TeensyRomApiClient.Json.cs` | Hand-written half of the client: enums as strings, response bodies kept for error messages. |
| `ApiPin.cs` | The pinned tr-web version and its address. |

## When the pin moves

1. Bump `ApiPin.Version` to the new tr-web version.
2. Check out that version of tr-web and run `pwsh scripts/regenerate-api-client.ps1 [-WebRepo <path>]`. It copies the spec and regenerates the client (needs the .NET SDK; no Java or Node).
3. Build and test.

Never hand-edit `Generated/`; change `nswag.json` or the hand-written partial instead and regenerate.
