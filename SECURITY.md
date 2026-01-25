# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x   | :white_check_mark: |

## Third-Party Dependencies

This project uses the following third-party libraries:

- **jQuery 3.7.1** (via CDN with SRI hash)
- **Bootstrap 5.3.3**
- **jQuery Validation**

These libraries are loaded from trusted CDNs with Subresource Integrity (SRI) hashes to prevent tampering.

## Known Issues

### jQuery XSS Vulnerabilities

GitHub CodeQL may flag XSS vulnerabilities in the jQuery library files located in `wwwroot/lib/jquery/`. 

**Status:** ? **Mitigated**

**Mitigation:**
1. jQuery is loaded from CDN with SRI hash in `_Layout.cshtml`
2. All user input is sanitized server-side before rendering
3. ASP.NET Core Razor Pages automatically HTML-encodes output
4. No user-controlled data is passed to jQuery's HTML manipulation functions

**CodeQL Configuration:**
- Third-party libraries are excluded from scanning via `.github/codeql/codeql-config.yml`
- Library files are marked as `linguist-vendored` in `.gitattributes`

## Reporting a Vulnerability

If you discover a security vulnerability in **our custom code** (not third-party libraries), please report it to:

**Email:** security@example.com

**Response Time:** We aim to respond within 48 hours.

## Security Best Practices Implemented

? **Input Validation**
- All user input validated with Data Annotations
- Strong password requirements enforced
- Email format validation

? **Output Encoding**
- Razor Pages automatically HTML-encode all output
- No use of `@Html.Raw()` with user input
- XSS protection enabled

? **Authentication & Authorization**
- ASP.NET Core Identity
- Secure session management
- Account lockout after failed attempts
- Password history tracking

? **Data Protection**
- Sensitive data encrypted at rest (AES-256)
- HTTPS enforced
- Secure cookies (HttpOnly, Secure, SameSite)

? **CSRF Protection**
- Anti-forgery tokens on all forms
- ValidateAntiForgeryToken on POST actions

? **SQL Injection Prevention**
- Entity Framework Core (parameterized queries)
- No raw SQL with user input

? **Password Security**
- PBKDF2 with HMAC-SHA256 (Identity default)
- Password history (prevent reuse)
- Password expiry policies
- Minimum/maximum password age

? **Audit Logging**
- All authentication events logged
- Failed login attempts tracked
- Password changes logged
- IP address and user agent captured

## Dependency Management

We regularly update dependencies to address security vulnerabilities:

```bash
# Check for outdated packages
dotnet list package --outdated

# Update all packages
dotnet outdated --upgrade
```

## Contact

For security concerns, please contact the security team at security@example.com.
