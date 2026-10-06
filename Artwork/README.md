# Artwork sources

`GitHub-Badge.svg` is the unmodified `GitHub Logos/SVG/GitHub_Lockup_White_Clearspace.svg`
from the [official GitHub logo download](https://brand.github.com/GitHub_Logos.zip), retrieved on 2026-10-07.
`GitHub-Badge.png` renders this vector at 128 pixels wide and links to this project's
GitHub repository in the Steam Workshop description.
See the [GitHub brand toolkit](https://brand.github.com/foundations/logo) for usage guidance.
GitHub and its logos are trademarks of GitHub, Inc.

Run `make preview` to render all PNG files.

## Steam Workshop previews

- `Workshop-Preview.svg` renders the 640 × 640 primary listing thumbnail. The
  uploader sends `Workshop-Preview.png` through the SteamCMD `previewfile` field.
- `Preview.svg` renders the 640 × 360 image at `About/Preview.png`. It is used
  in-game and as the first additional image in the Workshop detail gallery.

The primary thumbnail and detail gallery are separate Steam fields. Updating
`previewfile` alone replaces the listing thumbnail, not the additional gallery
image. When the wide artwork changes, update the first additional image through
Steam's image editor or [UpdateItemPreviewFile](https://partner.steamgames.com/doc/api/ISteamUGC#UpdateItemPreviewFile).
Use [AddItemPreviewFile](https://partner.steamgames.com/doc/api/ISteamUGC#AddItemPreviewFile)
only when no gallery image exists, to avoid duplicates. Verify both the author's
square item list and the detail page's wide gallery after changing preview images.
