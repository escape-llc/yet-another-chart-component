# eScapeLLC.UWP.Charts
Yet another chart component.  Also a play on the venerable "yacc" parser-generator tool.  Yes, I've used it, and know all about LR(1) parsers!

## Demo It!
The demo application in the solution is now available in [Windows Store](https://www.microsoft.com/store/apps/9P9XC6Z7R3BW) so you don't have to build it from source.

## Get It!
From *Package Manager Console*:
```
   PM> Install-Package eScapeLLC.UWP.Charts
```
[![NuGet version](https://badge.fury.io/nu/escapellc.uwp.core.svg)](https://badge.fury.io/nu/escapellc.uwp.charts)

[Package page on nuget.org](https://www.nuget.org/packages/eScapeLLC.UWP.Charts/)

## Or Build It
If you are rebuilding the demo application and/or test projects (e.g. because you forked or cloned this repo), you must re-generate the PFX file(s), because they are excluded from repository by `.gitignore`.

* In *Solution Explorer*, double-click the app manifest file.
* In the *Manifest Designer*, go to the *Packaging* tab.
* Click on *Choose Certificate*.
* Follow the instructions to make a new test certificate.
* Build away!

# Screen Shot
This is the current demo chart in the solution (subject to last-minute tweaking):
![yacc demo screen shot](http://escape-technology-llc.com/mobile/wp-content/gallery/main/yacc-chart-demo-18.png)

# API Documentation
The details of all the classes etc. used in YACC can be found at our [documentation page](http://escape-technology-llc.com/documentation/escapellc-uwp-core/) in the API section.

# More Info
Please check out the [Wiki](../../wiki) to learn more about the guts of YACC and rendering and linear algebra in general!

Please consult the [other](../../issues) [features](../../projects) of this github repository to find out the current state of affairs, [*or to contribute*](../../pulls).

# Nuget Releases
## 1.0.0
Initial release.

## 1.1.0
Some breaking changes, sorry about that!  As always, consult the demo chart XAML; it's currently the *reference*.

* For each following item, see the **Demo Chart XAML** for details.
* `PathStyle` et al is the biggest break.
   * make sure to apply a `Style` to your chart components, or you may get "invisible"!
* `HorizontalBand` new decoration.  Tracks two non-series values with a fill in between two horizontal rules.
* `Background` new decoration.  Basic fill/stroke path of data series area.
* `ValueAxisGrid` is now its own decoration, and not part of the `Axis` component.
* `MarkerSeries` now uses a normalized coordinate system (0..1, 0..1) for the marker geometry.
   * current the marker must be a `Geometry`.
   * marker center is (.5,.5).
* Major fixes to the linear algebra for the graphics, primarily impacting `MarkerSeries`.
* Lots of internal fixes/improvements you won't notice.
* Other new properties.  Sorry, rely on auto-complete in VS for now.

## 1.2.0
More features and fixes.

Since we were more organized this release, please see the [Milestone page](https://github.com/escape-llc/yet-another-chart-component/milestone/3?closed=1) for a list of included items.

## 1.3.0
More features and fixes.

See the [Milestone page](https://github.com/escape-llc/yet-another-chart-component/milestone/4?closed=1) for a list of included items.

## 1.4.0
More features and fixes.

See the [Milestone page](https://github.com/escape-llc/yet-another-chart-component/milestone/5?closed=1) for a list of included items.

## 1.4.1
Fixes due to sizing causing `ArgumentException`.

See the [Milestone page](https://github.com/escape-llc/yet-another-chart-component/milestone/6?closed=1) for a list of included items.
