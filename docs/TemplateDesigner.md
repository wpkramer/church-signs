# ChurchSigns — Template Designer Guide

This guide is for people who create **sign templates** for ChurchSigns.  
**Sign Makers** only select a template, paste data, and print or export PDF.  
**Template Designers** prepare the SVG (and optional metadata) those flows use.

You should be comfortable exporting SVG from a design tool and editing the file in a text editor when needed.

---

## What a template is

| Piece | Role |
|--------|------|
| **`.svg` file** | Artwork plus placeholders for variable text and colors |
| **`.json` sidecar** (optional but recommended) | Sample preview values, paper size, orientation |
| **Category** | App grouping (e.g. Awana, Children, Miscellaneous) |

**Provided templates** ship with the app. **Custom templates** are stored after **Import** (local app data).

---

## Placeholders

Variable content uses double braces in the SVG source:

```text
{{FieldName}}
```

### Text

```xml
<text x="200" y="100" text-anchor="middle" font-size="24">{{Name}}</text>
```

### Color

Put the placeholder in a color attribute (or in `style`):

```xml
<rect x="0" y="0" width="400" height="80" fill="{{BadgeColor}}" />
<text fill="{{Color}}">{{Name}}</text>
```

Include **`color`** or **`colour`** in the field name (`Color`, `BadgeColor`, `Team Colour`) so the app treats the field as a color (preview defaults, paste behavior).

### Guidelines

- Prefer clear names: `Name`, `Room`, `Teacher`, `BadgeColor`.
- Headers in the Sign Maker’s spreadsheet should match these names (spacing and capitalization can vary slightly; the app fuzzy-matches).
- Spaces in names are allowed (`{{Last Name}}`) but matching is easier without them.
- ChurchSigns merges by replacing `{{...}}` in the file text. Do **not** rely on XML `id` attributes for merging.

---

## Authoring workflow

1. Design the sign in Affinity Designer, Inkscape, Illustrator, or similar, at the intended aspect ratio (portrait or landscape).
2. Export **SVG**.
3. Open the SVG in a text editor.
4. Replace fixed sample text and fills with `{{FieldName}}` placeholders.
5. Confirm the root element is `svg` and that **viewBox** (or numeric width/height) reflects the real aspect ratio. The app uses that when orientation is **Default**.
6. Optionally create a sidecar `.json` (see below).
7. In ChurchSigns, enable **Designer** and **Import**, or add files under the app’s packaged `Templates/<Category>/` folder for provided templates.

### SVG tips

- Prefer an explicit **viewBox** (for example `viewBox="0 0 1100 850"`).
- Keep variable text as real `<text>` (or text that survives export), not only outlined paths.
- Center text with `text-anchor="middle"` and appropriate `x` / `y`.
- Multi-line content is usually multiple tspans or separate fields (`{{Line1}}`, `{{Line2}}`). Full automatic wrapping is limited in the print/PDF pipeline.

---

## Sidecar JSON

Name the sidecar like the SVG:

```text
LeaderSign.svg
LeaderSign.json
```

### Example

```json
{
  "version": 1,
  "printOrientation": "Default",
  "templateMediaSize": "Letter",
  "fields": {
    "Name": "John Doe",
    "Room": "101",
    "BadgeColor": "#000000"
  }
}
```

| Property | Meaning |
|----------|---------|
| `version` | Sidecar format version (`1`) |
| `printOrientation` | `Default` (from SVG aspect), `Portrait`, or `Landscape` |
| `templateMediaSize` | `Letter` (8.5×11), `Legal` (8.5×14), `Tabloid` (11×17) |
| `fields` | Sample values for thumbnails and designer preview only |

If the JSON is missing, ChurchSigns uses Letter, derives orientation when possible, empty text samples, and `#000000` for color-like fields.

**Note:** Sidecar `fields` are not the live data Sign Makers paste. Paste data is in-memory only for the session.

---

## Multi-sign vs single-sign

If your build supports template modes:

| Mode | Paste shape | Result |
|------|-------------|--------|
| **Multi-sign** (typical) | Header row + one **row per sign** | Many signs (name badges, door cards, …) |
| **Single-sign** | Two columns: **field name** \| **value** | One sign with many fields (e.g. seating chart) |

Set the mode in the designer UI or sidecar according to your app version so Sign Makers get the correct paste behavior.

---

## Import and export

### Import (Designer mode)

- **`.svg`** — one template (default sidecar if none provided).
- **`.zip`** — one or more `.svg` files, each optionally paired with a `.json` of the same base name.

Templates are stored under a **category**. Re-importing the same category and file name **overwrites** the previous custom template.

### Export

Creates a **zip** containing the SVG and its JSON sidecar, suitable for sharing or source control.

---

## How Sign Makers use your template

1. Select the template in the left column.  
2. Paste from Google Sheets or Excel (header names ≈ your field names).  
3. Adjust column → field mapping if auto-match misses.  
4. Print or export PDF.

They should not need to edit SVG. Clear field names and a sensible sidecar make that path easier.

---

## Checklist before sharing

- [ ] Root element is `svg`; file opens in a browser or editor  
- [ ] Variable content uses `{{FieldName}}`  
- [ ] Color placeholders sit in `fill` / `stroke` (or `style`) and names contain `color` or `colour`  
- [ ] viewBox or dimensions match intended portrait/landscape  
- [ ] Sidecar samples look correct in the app thumbnail  
- [ ] Media size is correct (Letter is the usual default)  
- [ ] Smoke-test: small paste, then Print or Export to PDF  

---

## Avoid

- Depending on element `id`s for mail-merge  
- Publishing templates with empty color attributes instead of `{{BadgeColor}}`  
- Putting real personal data into sidecar sample fields  
- Assuming HTML / `foreignObject` layout will match print output (the app rasterizes with Skia)

---

## Minimal example

**LeaderSign.svg**

```xml
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1100 850">
  <rect width="1100" height="850" fill="#f5f5f5"/>
  <rect x="40" y="40" width="1020" height="120" fill="{{BadgeColor}}"/>
  <text x="550" y="120" text-anchor="middle" font-size="48" fill="#ffffff">{{Name}}</text>
  <text x="550" y="400" text-anchor="middle" font-size="32" fill="#333333">{{Room}}</text>
</svg>
```

**LeaderSign.json**

```json
{
  "version": 1,
  "printOrientation": "Landscape",
  "templateMediaSize": "Letter",
  "fields": {
    "Name": "Jane Smith",
    "Room": "Fellowship Hall",
    "BadgeColor": "#1E4D8C"
  }
}
```

---

## See also

- Sign Maker flow: select template → paste → print / PDF  
- Project repository: https://github.com/wpkramer/church-signs  
```
