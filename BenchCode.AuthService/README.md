# Authentication API

AuthService supports public user registration, the OpenIddict password grant, and refresh tokens:

- `POST /api/users` registers a username, email, and password.
- `POST /connect/token` accepts `application/x-www-form-urlencoded` with `grant_type=password`, `client_id=compose-web-app`, `username`, `password`, and `scope=benchcode-api offline_access`.
- Send the returned access token as `Authorization: Bearer <access_token>` to protected endpoints such as `GET /api/users/me`.
- Refresh with `grant_type=refresh_token`, `client_id=compose-web-app`, and the returned `refresh_token`.

In Scalar, open `POST /connect/token`, choose the form fields, and set:

```text
grant_type = password
client_id = compose-web-app
username = the username used at registration
password = the password used at registration
scope = benchcode-api offline_access
```

Click **Execute**. Copy `access_token` from the response and use it in the **Authorize** dialog or as a Bearer token for protected endpoints. The Compose client is registered as a public client: it has no client secret embedded in the browser app.

Access tokens use the `benchcode-api` audience and are signed JWTs so independently deployed backend services can validate them with the shared extension in [ServiceDefaults](../BenchCode.ServiceDefaults/README.md). Do not put sensitive data in access-token claims.

Configure `OpenIddict:Issuer` as the externally reachable issuer URL in every environment. Development uses the HTTPS URL in `appsettings.Development.json`. Outside Development, provide separate PKCS#12 signing and encryption certificates using:

- `OpenIddict__Certificates__Signing__Path` and `OpenIddict__Certificates__Signing__Password`
- `OpenIddict__Certificates__Encryption__Path` and `OpenIddict__Certificates__Encryption__Password`

Provide certificate passwords through a secret store/environment variables, not committed settings. `AuthDbContext` currently initializes its schema with `EnsureCreated`; use a coordinated database migration before evolving the schema in an existing deployment.
