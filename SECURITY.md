# Security Policy

## Supported Versions

Only the latest stable version of the project is actively supported. Security patches will be provided for this version only.

| Version | Supported          |
| ------- | ------------------ |
| 2.x.x   | :white_check_mark: |
| < 2.0.0 | :x:                |

## Reporting a Vulnerability

We take all security bugs in this project seriously. Thank you for improving the security of our project. We appreciate your efforts and responsible disclosure and will make every effort to acknowledge your contributions.

Please report any security vulnerabilities by emailing Arda Terekeci at **aterekeci@gmail.com**.

Please include the following details with your report:

*   Description of the vulnerability
*   Steps to reproduce the vulnerability
*   Impact of the vulnerability
*   Any potential mitigations

We will review your report and get back to you, typically within 48 hours. We will do our best to keep you informed about our progress in resolving the issue.
## Runtime and input policy

Use serviced .NET 8/9/10 runtimes. The net11.0 asset is validated against .NET 11 RC1; final .NET 11 validation will be documented separately. Structural bounds protect stored trees but are not an input byte or transient allocation quota; apply request size limits at application boundaries. Custom JSON converters execute application code and must be trusted. The public API uses reflection-based serialization; Native AOT support is not claimed.
