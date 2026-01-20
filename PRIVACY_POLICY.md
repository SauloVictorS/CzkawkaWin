# Privacy Policy

**Last Updated:** January 2025

## Introduction

CzkawkaWin ("the Application") is a Windows desktop application developed by Saulo VS ("we", "us", or "our"). This Privacy Policy explains how the Application handles your information.

## Summary

**CzkawkaWin does not collect, store, transmit, or share any personal data.** The Application operates entirely offline on your local computer.

## Information We Do NOT Collect

CzkawkaWin does **not** collect any of the following:

- Personal identification information (name, email, address, etc.)
- Device identifiers or hardware information
- Usage statistics or analytics
- Location data
- Network activity or browsing history
- Any data from your files beyond what is necessary for local duplicate detection

## How the Application Works

### Local Processing Only

CzkawkaWin is a **100% offline application**. All operations are performed locally on your computer:

- **File Scanning**: The Application scans directories you specify to find duplicate files. This scanning is performed entirely on your local machine.
- **File Analysis**: File metadata (size, hash, name) is analyzed locally and never transmitted anywhere.
- **Results Storage**: Scan results are stored locally in JSON format on your computer.
- **Settings**: Application preferences and scan profiles are stored locally on your device.

### Third-Party Components

CzkawkaWin integrates with the following open-source tools, which also operate locally:

- **[Czkawka CLI](https://github.com/qarmin/czkawka)**: Used for duplicate file detection. Operates entirely offline.
- **FFmpeg**: Used for video thumbnail generation and metadata extraction. Operates entirely offline.

These components do not transmit any data over the internet.

## Data Storage

All data created or used by CzkawkaWin remains on your local computer:

| Data Type | Location | Purpose |
|-----------|----------|---------|
| Application Settings | Local AppData folder | Store user preferences |
| Scan Profiles | Local AppData folder | Save scan configurations |
| Scan Results | User-specified location | Export duplicate file reports |
| Temporary Files | System temp folder | Video thumbnails (automatically cleaned) |

## Internet Access

The Application requests the `internetClient` capability for potential future features (such as checking for updates), but **currently does not transmit any data over the internet**. All file scanning and analysis operations are performed locally.

## Your Rights and Control

Since CzkawkaWin does not collect any personal data:

- There is no personal data to access, modify, or delete
- You have full control over your local files and scan results
- You can delete all Application data by uninstalling the Application and removing its local data folder

## Children's Privacy

CzkawkaWin does not collect any personal information from anyone, including children under 13 years of age.

## Security

While CzkawkaWin does not collect or transmit data, we are committed to maintaining the security and integrity of the Application:

- The Application is open source and available for review on [GitHub](https://github.com/SauloVictorS/CzkawkaWin)
- No network connections are made during normal operation
- All file operations are performed with standard Windows security permissions

## Changes to This Privacy Policy

We may update this Privacy Policy from time to time. Any changes will be posted on this page with an updated revision date. We encourage you to review this Privacy Policy periodically.

## Open Source

CzkawkaWin is open source software licensed under the MIT License. You can review the source code at any time:

- **Repository**: [https://github.com/SauloVictorS/CzkawkaWin](https://github.com/SauloVictorS/CzkawkaWin)

## Contact Us

If you have any questions about this Privacy Policy or the Application, please:

- Open an issue on [GitHub Issues](https://github.com/SauloVictorS/CzkawkaWin/issues)
- Contact the developer through the GitHub repository

---

## Consent

By using CzkawkaWin, you acknowledge that you have read and understood this Privacy Policy. Since the Application does not collect any personal data, no additional consent is required for data processing.

---

*This privacy policy applies to CzkawkaWin version 1.0.0 and later.*
