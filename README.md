# ChurchSigns

**Simple WinUI app for churches to create and print signs and labels from SVG templates and spreadsheet data.**

ChurchSigns helps churches quickly generate name badges, room signs, welcome signs, event materials, and other printed items. It is designed to be easy for non-technical volunteers while remaining flexible for those who want to create their own templates.

## Vision

- Stay focused on the church use case
- Prioritize simplicity over advanced features
- Keep personal data (names, etc.) in memory only — never saved to disk
- Use existing design tools (Affinity, Inkscape, Illustrator, etc.) for templates instead of building a full vector editor
- Make the most common workflow (paste from Google Sheets → preview → print) as frictionless as possible

## Planned Features

- Browse templates by ministry/category tags
- Import data by pasting from Google Sheets (or CSV/Excel)
- Automatic and manual field matching (`{{Field Name}}` style placeholders in SVG)
- Live preview
- Direct printing
- Built-in starter templates for common church needs
- Ability to import custom SVG templates

## Privacy

ChurchSigns is intentionally designed so that personal data (such as teacher or volunteer names) is **never persisted**.

- Data exists only in memory for the current print job
- Closing the job or the application discards the data
- No accounts, no cloud sync, and no local database of names

## Tech Stack

- **UI**: WinUI 3 + Windows App SDK
- **Language**: C#
- **Architecture**: Shared library (`ChurchSignsLib`) + main app + unit tests
- **Target**: Microsoft Store (packaged MSIX)

## Solution Structure
