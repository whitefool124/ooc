---
name: funplay-unity-ui-composition
description: Build and revise responsive Unity uGUI mobile interfaces, including portrait and landscape layouts, safe areas, prefabs, auto layout, scrolling, text, input, animation, and performance validation.
---
<!-- Funplay Unity MCP managed project skills -->
<!-- Funplay Unity MCP skill version: unity-ui-composition@1.0.4 -->

# Unity UI Composition

Use this built-in skill when creating, assembling, adapting, reviewing, or fixing Canvas-based Unity UI, especially mobile screen or popup prefabs that must work across aspect ratios, notches, tablets, localization, and runtime state changes.

## Operating Loop

1. Inspect before editing.
   - Confirm the active scene, Canvas render mode, Canvas Scaler settings, EventSystem and input module, target orientations, design resolution, safe-area policy, and relevant prefab asset paths.
   - Inspect the existing hierarchy, anchors, pivots, offsets, layout controllers, sibling order, Canvas sorting, serialized references, animation targets, and Prefab overrides.
   - Inspect representative screens and prefabs to determine whether `UnityEngine.UI.Text` or `TextMeshProUGUI` is the project's prevailing text component, and inspect existing visual-effect components and material presets before introducing a new UI effect.
   - Treat screenshots and design coordinates as visual intent, not as permission to replace a working hierarchy.
2. Classify each region.
   - Mark art as full-bleed or safe-area content.
   - Mark placement as fixed to an edge or corner, stretched between regions, content-sized, repeated-layout content, scrollable content, modal, or world-space UI.
   - Decide which component owns each axis. One axis must not be driven concurrently by a Layout Group, ContentSizeFitter, AspectRatioFitter, animation, and manual code.
3. Make the smallest coherent change.
   - Preserve the prefab root, existing children, components, names, serialized references, animation bindings, and Prefab overrides unless a specific replacement is required.
   - Modify only the necessary RectTransforms, components, fields, and children. Do not recreate an entire UI or GameObject prefab unless the user explicitly requests a rebuild.
   - Author reusable user-facing screens, panels, and controls as prefabs with their hierarchy and component references wired in the Editor, then instantiate and bind data at runtime. Do not move a stable UI hierarchy into procedural runtime construction merely for implementation convenience.
   - Use Unity MCP or Unity Editor APIs for `.prefab`, `.unity`, and `.asset` changes; never patch Unity YAML as text.
4. Read back and validate.
   - Read exact hierarchy, anchors, offsets, sizes, sprites, text settings, raycast state, sorting, and references back from Unity.
   - Test layout, input, safe area, localization, animation interruption, close and reopen state, and runtime data changes.
   - Capture screenshots at representative aspect ratios for static layout; use a short `record_game_view` clip when correctness depends on an animation or interaction sequence. Use a real device build for performance and platform behavior before claiming device validation.

## Component Selection

| Component | Use it for | Configure deliberately | Avoid |
| --- | --- | --- | --- |
| `Canvas` | Root render and sorting space | Use Screen Space - Overlay for ordinary HUD and menus, Screen Space - Camera when camera composition or scene interleaving is required, and World Space only for UI that belongs in the 3D world | Adding independent canvases only to organize folders; leaving Event Camera unset in Camera or World Space modes |
| `CanvasScaler` | Converting a design resolution to screen-space scale | For mobile screen UI, normally use Scale With Screen Size and a documented portrait or landscape reference resolution; choose Match from actual width and height constraints | Assuming Match `0.5` solves every aspect ratio or relying on Constant Physical Size without validating device DPI |
| `RectTransform` | Parent-relative position and size | Set anchors first, then pivot and offsets; keep local scale at one; use stretch anchors for flexible regions | Using anchoredPosition from one screenshot as universal placement or using Transform scale as a layout tool |
| `HorizontalLayoutGroup` / `VerticalLayoutGroup` | Dynamic rows, columns, button rails, and variable-length lists | Set padding, spacing, child size control, expansion, and child `LayoutElement` intent | Applying a Layout Group to hand-composed full-screen art or manually positioning driven children |
| `GridLayoutGroup` | Uniform inventory, level, or card cells | Choose cell size, spacing, start axis, and a row or column constraint | Expecting child preferred sizes to change grid cells; GridLayoutGroup assigns fixed cells |
| `LayoutElement` | Declaring minimum, preferred, flexible, or ignored layout behavior | Use it to override an Image, text, or nested group's layout contribution and to make selected siblings flexible | Adding it without selecting the properties that should override layout input |
| `ContentSizeFitter` | Making the current RectTransform follow its content on one or two axes | Prefer a single required axis; set the pivot to control growth direction; allow deferred layout unless immediate measurement is truly required | Putting it on every child controlled by a parent Layout Group or writing the same driven size manually |
| `AspectRatioFitter` | Preserving aspect for an isolated preview, card art, or media surface | Use Fit In Parent for letterboxing or Envelope Parent for cover behavior | Treating it as general safe-area or screen-aspect adaptation, or combining it with another controller on the same axis |
| `Image` | Sprite UI, icons, frames, progress fills, and nine-sliced controls | Use Simple for fixed art, Sliced for resizable bordered panels and buttons, Tiled for repeatable patterns, and Filled for progress or radial values | Stretching bordered art as Simple, leaving decorative graphics as Raycast Target, or using a unique material without need |
| `RawImage` | Arbitrary Texture, RenderTexture, camera, video, downloaded, or generated texture content | Preserve the source aspect and manage texture lifetime explicitly | Using RawImage for ordinary Sprite UI that should atlas and batch with other Images |
| `UnityEngine.UI.Text` | Text in an established legacy uGUI project or screen family | Use it only after inspection shows it is the prevailing project convention; match the existing Font, material, alignment, line spacing, overflow, and localization behavior | Introducing it into a new project, mixing it casually into a TMP-based screen, or converting existing labels without checking layout and serialized references |
| `TextMeshProUGUI` | Text in an established TMP project and the default for a new project with no existing text convention | Match the project's font assets and material presets; set wrapping, alignment, overflow, fallback fonts, and localization limits; constrain Auto Size to a narrow range; when the design explicitly shows a text effect, use TMP's own component and shader-material controls | Replacing an established `Text` component merely to modernize, continuous Auto Size on rapidly changing text, or shipping without required CJK and symbol glyphs |
| TMP font material / material preset | An outline, underlay or shadow, glow, face dilation, softness, or other text treatment explicitly visible in the design for a `TextMeshProUGUI` project | For a simple outline set the TMP component's `outlineColor` and `outlineWidth`; for underlay, glow, or other shader effects reuse an approved project preset or create a dedicated preset or material instance and verify it with fallback fonts | Adding a uGUI `Outline` or `Shadow` to TMP text by habit, changing a shared `fontSharedMaterial` and unintentionally restyling other labels, or enabling effects absent from the design |
| `Outline` / `Shadow` / similar `BaseMeshEffect` | A specifically designed effect on legacy `UnityEngine.UI.Text` or another Graphic, when required by the design or established project style | Reuse the project's approved style and keep effect distance, color, alpha, and stacking minimal; verify legibility and cost on target hardware; prefer TMP-native effects when the text component is `TextMeshProUGUI` | Adding generic polish by default, stacking effects, applying them broadly, or using them to compensate for weak contrast or incorrect layout; these effects duplicate UI geometry and increase overdraw |
| `TMP_InputField` | Editable TMP text | Author and validate a prefab with `textComponent`, `textViewport`, `placeholder` when used, target Graphic, navigation, and input settings already serialized; instantiate the prefab and bind data or listeners at runtime | Rebuilding a stable input hierarchy in code; if dynamic construction is genuinely required, never add it to an active GameObject and bind `textComponent` afterward because affected TMP versions create the caret renderer in `OnEnable` only when that reference is already present |
| `ScrollRect` | Drag or wheel scrolling through content larger than a viewport | Use `ScrollRect -> Viewport + RectMask2D -> Content`, reference both Viewport and Content, enable only required axes, and choose Clamped or Elastic intentionally | Unrestricted movement without recovery, deeply nested competing scroll axes, or instantiating thousands of live rows without virtualization |
| `RectMask2D` | Rectangular clipping in 2D Canvas UI | Prefer it for scroll viewports and rectangular reveal areas | Using stencil `Mask` for a simple rectangle |
| `Mask` | Clipping to a non-rectangular Graphic shape | Use only when the shape matters and account for stencil and material cost | Deeply nested masks or using it where RectMask2D is sufficient |
| `CanvasGroup` | Fading and enabling or disabling a whole panel | Change alpha, interactable, and blocksRaycasts together according to visible state; decide whether parent groups apply | Setting alpha to zero while leaving an invisible panel interactive or raycast-blocking |
| `Button` and other `Selectable` controls | Click, toggle, slider, dropdown, and navigation behavior | Put the main Raycast Target on the interactive root, set Target Graphic and navigation, and add and remove runtime listeners symmetrically | Multiple child Raycast Targets for one control, duplicate listeners, or visual-only disabled states |
| `EventSystem` and `GraphicRaycaster` | Routing pointer, touch, submit, cancel, and navigation events | Keep one EventSystem and one active matching input module; use `InputSystemUIInputModule` with the Input System; enable raycast only where required | A second EventSystem in additive scenes or physics blocking checks when they are unnecessary |

## Canvas And Layering

- Use a small number of semantic layers such as Background, Screen, HUD, Overlay, Modal, Loading, and Debug. Make each layer a full-stretch RectTransform and define sibling or sorting order once.
- Let opaque or decorative backgrounds bleed to the physical screen edges. Put critical labels and all interactive controls under a separate SafeAreaRoot.
- When Modal or Loading UI is visible, block gameplay input explicitly; a visible scrim alone does not prove input is blocked.
- Keep one stable scrim per popup layer when a popup stack owns it. Restore the previous popup and its input state when the top popup closes.
- Distinguish Hide from Close. Hide can retain a cached instance; Close must release instantiated assets, handles, listeners, and transient state.
- Split static and frequently changing UI only when profiling shows rebuild cost. Nested canvases isolate rebuilds but prevent batching across canvas boundaries, so do not create one Canvas per widget.

## Canvas Scaler And RectTransform Rules

- Treat the reference resolution as design coordinates, not a list of supported physical resolutions. A proven portrait baseline is `720 x 1559`; a proven landscape baseline is `1559 x 720`.
- Start with Scale With Screen Size and Match `0.5` when width and height are equally important, then verify. Move Match toward width when horizontal design width must remain stable, or toward height when vertical design height must remain stable.
- Use anchors to express attachment: top bars to top stretch, bottom actions to bottom or bottom stretch, edge buttons to their corner, and center gameplay viewports to stretch between reserved regions.
- Set anchors before recording offsets. With separated anchors, `sizeDelta` is the delta relative to the anchor rectangle, not the final absolute size.
- Resize UI through RectTransform width, height, anchors, and offsets; leave localScale at one. Animate a child named Visual or Container when the root is layout-driven.
- Respond to `OnRectTransformDimensionsChange` or an equivalent resolution and orientation signal when layout contains calculated page widths, aspect branches, or safe-area anchors. Do not poll and rewrite every RectTransform every frame.
- Prefer `LayoutRebuilder.MarkLayoutForRebuild` for deferred updates. Use `Canvas.ForceUpdateCanvases` or `LayoutRebuilder.ForceRebuildLayoutImmediate` only when code must measure the final layout in the same operation, never as a routine per-frame fix.

## Safe Area

- Read `Screen.safeArea` in screen pixels and convert both minimum and maximum corners to normalized anchors. Reapply when screen dimensions, orientation, or safe area changes; do not cache only a top inset.
- A minimal uGUI conversion is:

```csharp
Rect safe = Screen.safeArea;
safeAreaRoot.anchorMin = new Vector2(
    safe.xMin / Screen.width,
    safe.yMin / Screen.height);
safeAreaRoot.anchorMax = new Vector2(
    safe.xMax / Screen.width,
    safe.yMax / Screen.height);
safeAreaRoot.offsetMin = Vector2.zero;
safeAreaRoot.offsetMax = Vector2.zero;
```

- Guard zero screen dimensions and avoid duplicate application when nothing changed.
- Check `PlayerSettings.Android.renderOutsideSafeArea`. If rendering outside is disabled, the Player window can already be fitted to the safe area and `Screen.safeArea` can equal the full Player window; do not apply a second inset blindly.
- In portrait, verify top cutout and bottom home-indicator or navigation areas. In landscape, verify both left and right cutouts in Landscape Left and Landscape Right.
- For edge art assembled from nested images, particles, labels, or Spine content, wait until layout and final offsets are applied, then use `RectTransformUtility.CalculateRelativeRectTransformBounds(parent, visualRoot)` to clamp the complete visual bounds inside the allowed safe rectangle.

## Portrait Mobile Pattern

- Organize the screen as Top, Center, and Bottom regions. Anchor persistent status and currency UI to Top; navigation, primary actions, and skill buttons to Bottom; stretch the game or page viewport through Center.
- Reserve top and bottom space with offsets on the stretched center viewport instead of giving the viewport a fixed height.
- Let additional height on tall phones expand the center region. Do not multiply every vertical coordinate by the screen aspect ratio.
- For horizontally paged home screens, compute each page from the current viewport width and recompute content width and selected-page position when dimensions change.
- Keep centered popup content within a safe maximum height. Use scrolling for localized or data-driven content that can exceed that height.

## Landscape Mobile Pattern

- Organize the screen as Left, Center, Right, plus stable corners. Put high-frequency gameplay content in Center and distribute controls so neither side becomes a single crowded column.
- Use 16:9 as a common gameplay baseline, but branch deliberately for ultrawide, 16:10, and 4:3 tablet layouts. Anchors handle attachment; a small aspect-aware layout policy handles genuine composition changes.
- Keep camera framing separate from Canvas scaling. A narrow landscape viewport may require a larger orthographic size or alternate camera composition to preserve world-space gameplay even when the Canvas itself is correct.
- Use background cover scaling or phone and tablet background variants when one crop cannot preserve the art direction across 16:9 and 4:3.
- Reposition only the controls whose composition genuinely changes at tablet aspect ratios. Do not fork the entire screen prefab when a few region offsets or constraints are sufficient.

## Auto Layout And Dynamic Content

- Remember the allocation order: minimum size, then preferred size, then flexible size. Use `LayoutElement` to state which sibling can consume extra space.
- A Layout Group drives its children. Do not manually edit a driven child position or size and expect it to persist after the next rebuild.
- A ContentSizeFitter drives its own RectTransform and expands around its pivot. Use a top pivot for content that must grow downward and a left pivot for content that must grow rightward.
- Do not put ContentSizeFitter on children whose RectTransforms are already controlled by the parent Layout Group. Disable Child Force Expand and use child layout input instead.
- GridLayoutGroup ignores child minimum, preferred, and flexible dimensions and assigns the configured fixed cell size. Use a different layout or custom controller for variable-sized grid cells.
- Keep layout nesting shallow. Repeated layout invalidation walks the hierarchy; batch model changes, update content, then request one rebuild.

## Images, Text, Scrolling, And Input

- Use Sprite borders and Image Type Sliced for scalable button and panel frames. Keep ornamental children non-raycastable.
- Use Sprite Atlas for compatible UI sprites, platform-specific texture overrides, sensible maximum sizes, and no mipmaps for ordinary screen-space UI unless a measured use case needs them.
- Treat large full-screen images separately from small control atlases. Verify memory, compression artifacts, overdraw, and crop behavior on target hardware.
- Before adding a label, inspect representative UI prefabs and scenes rather than inferring the text system from package availability. Preserve the component type on existing labels and use the text component that is most common in the relevant project or screen family. If the project is new and has no established convention, default to `TextMeshProUGUI`.
- Do not opportunistically migrate `UnityEngine.UI.Text` to `TextMeshProUGUI`, or the reverse, while composing unrelated UI. Such a migration can change preferred sizes, wrapping, materials, fallback behavior, animation bindings, and serialized component references and requires separate validation.
- In TMP projects, use font fallback chains for CJK, symbols, and localized glyphs. Keep common glyphs in the primary asset and verify fallback material appearance and draw-call impact.
- Prefer wrapping, truncation, or a known layout expansion policy over broad Auto Size ranges. TMP Auto Size performs repeated layout passes and is unsuitable for frequently changing counters or timers.
- Do not add `Outline`, `Shadow`, or another `BaseMeshEffect` merely because a control looks unfinished. Require an explicit design need or a verified existing project style, prefer the existing shared prefab or TMP material preset when applicable, avoid stacked effects, and verify the extra geometry and overdraw.
- When the reference image or design explicitly shows an outline or another font effect and the project uses `TextMeshProUGUI`, reproduce the visible treatment through the TMP component rather than omitting it or attaching a uGUI mesh effect by habit. Use `outlineColor` and `outlineWidth` for a simple outline; use an existing approved TMP material preset, or a dedicated preset or material instance, for underlay or shadow, glow, face dilation, softness, and other shader effects.
- Treat TMP material scope as part of the edit. Do not modify a shared `fontSharedMaterial` when the effect is local to one label or prefab because every user of that material may change. Reuse a matching project preset when one exists; otherwise create a deliberately scoped preset or instance, preserve the font atlas and fallback chain, and verify the result against the reference at target resolution.
- Prefer a prefab for `TMP_InputField` and other stable controls so hierarchy, references, navigation, styling, localization, and focus behavior are inspectable before Play Mode. Runtime code should instantiate the prefab and supply data and listeners, not recreate its child objects and component wiring.
- Only when procedural construction is explicitly required, treat the first enable of a runtime-created `TMP_InputField` as an initialization boundary. Create and wire its text hierarchy while the root is inactive (or the component is disabled), assign at least `textComponent` and `textViewport` plus `placeholder` when used, then enable it. In affected TMP versions, `OnEnable` creates the cached `Caret` renderer only when `textComponent` is already bound; assigning the property after that first enable does not retroactively create it.
- If a dynamically constructed `TMP_InputField` accepts text but shows no insertion caret, inspect whether a `Caret` / `TMP_SelectionCaret` object was created and whether `textComponent` was assigned before first enable. After wiring the missing references, disable and re-enable the field to run initialization again; then verify focus, blinking caret, selection highlight, placeholder state, and editing in Play Mode.
- For a vertical ScrollRect, top-anchor the Content and set its pivot to the top so growth is predictable. Preserve the normalized position intentionally when refreshing content.
- Use RectMask2D for rectangular viewports. Use Mask only when the clipping shape must follow a Graphic.
- Give touch controls a project-defined minimum hit area even when the visible art is smaller. Use one transparent or visible root Graphic or raycast padding rather than making every child Image a target.
- Add listeners once and remove them in the matching lifecycle. Disable interaction while entrance or exit animation makes a control visually unavailable.

## Animation And Prefab Safety

- Animate a popup Container or Visual child while leaving the full-screen scrim stable. This avoids scaling the raycast blocker and avoids fighting layout-driven roots.
- Before replaying an animation, kill or cancel the prior sequence and restore a deterministic base position, scale, alpha, interactable, and blocksRaycasts state.
- Use unscaled time for menu, pause, modal, and loading animations that must continue while gameplay time is zero.
- Preserve existing prefab objects by default. Replacing an asset at the same path can preserve the asset GUID while still changing child or component file IDs, breaking animation bindings, serialized references, Prefab Variants, and Scene overrides.
- Prefer serialized references or stable binding components. Use `Transform.Find` only for a verified stable hierarchy and fail clearly if it is missing; never silently create an alternate hierarchy.
- Use semantic names such as SafeAreaRoot, TopBar, ContentViewport, BottomActions, Visual, and Label. Replace ambiguous default names only when doing so will not break bindings, and update references atomically.

## Performance And Validation

- Profile before restructuring. Common uGUI bottlenecks are overdraw, Canvas batch rebuilds, repeated layout rebuilds, raycast candidates, text mesh generation, and excess materials or textures.
- Separate mostly static UI from high-frequency counters, timers, scrolling content, or animations when profiling justifies the extra Canvas. Co-locate elements that change together.
- Disable Raycast Target on decorative Images and TMP text. An active Graphic Raycaster tests eligible Graphics and raycast filters along their hierarchy.
- Avoid hiding large inactive screens only with alpha zero; they can still render or receive input depending on CanvasGroup state. Use the project's hide or pooling policy and measure reopen cost.
- Validate portrait at 16:9, 19.5:9 or 20:9, a cutout phone, and a portrait tablet. Validate landscape at 16:9, ultrawide, 16:10, 4:3, and both cutout sides.
- In every profile, verify full-bleed art, safe interactive content, text overflow and fallback glyphs, scroll bounds, modal input blocking, touch hit areas, selection navigation, animation interruption, and close and reopen state.
- Use `capture_game_view` for static composition, text fit, and before/after comparisons. Use a short `record_game_view` clip for behavior a still image cannot establish, such as popup transitions, scroll inertia, caret blinking, interrupted animations, or repeated close and reopen actions; record only the sequence relevant to the change.
- For a clip, finish compilation, enter Play Mode and wait for MCP recovery, keep the Game tab visible at a fixed resolution, then start recording before performing the interaction. Save `data.recording_id`, pass it to `action=status` or `action=stop`, and wait for `data.ready=true` before reviewing the local MP4. The Unity MCP Workflow skill describes supported Editors and failure handling. If recording or video viewing is unavailable, report that limitation; screenshots and hierarchy readback alone do not prove timing or transition correctness.
- Review intermediate frames as well as the final state: look for clipping or layout jumps, stuck raycast blocking, input leaking through a modal, and interruption or reopen state. Pair the clip with component-state readback and actual input checks; a visual result alone cannot prove event routing. Recording is silent and adds overhead, so it cannot validate audio or replace Profiler and real-device performance checks.
- Use Device Simulator for layout, safe-area, orientation, and basic single-touch checks. It does not simulate target CPU, GPU, memory, rendering backend, native plugins, or multitouch; use representative device builds for performance and final interaction validation.

## Official Unity References

- [Canvas render modes and nesting](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/class-Canvas.html)
- [CanvasScaler API and Match behavior](https://docs.unity.cn/Packages/com.unity.ugui%402.0/api/UnityEngine.UI.CanvasScaler.html)
- [RectTransform](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/class-RectTransform.html) and [multi-resolution UI](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/HOWTO-UIMultiResolution.html)
- [Auto Layout](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/UIAutoLayout.html), [LayoutElement](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-LayoutElement.html), and [ContentSizeFitter](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-ContentSizeFitter.html)
- [ScrollRect](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-ScrollRect.html), [RectMask2D](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-RectMask2D.html), and [Mask](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-Mask.html)
- [Image](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-Image.html), [CanvasGroup](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/class-CanvasGroup.html), and [Selectable navigation](https://docs.unity.cn/Packages/com.unity.ugui%402.0/manual/script-SelectableNavigation.html)
- [TextMeshPro UI text and Auto Size](https://docs.unity.cn/Packages/com.unity.textmeshpro%403.2/manual/TMPObjectUIText.html) and [fallback fonts](https://docs.unity.cn/Packages/com.unity.textmeshpro%404.0/manual/FontAssetsFallback.html)
- [Screen.safeArea](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-safeArea.html), [relative RectTransform bounds](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RectTransformUtility.CalculateRelativeRectTransformBounds.html), and [Device Simulator](https://docs.unity3d.com/6000.0/Documentation/Manual/device-simulator-introduction.html)
- [Sprite Atlas](https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/atlas/create-sprite-atlas.html), [platform texture overrides](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TextureImporter-type-specific.html), and [official uGUI optimization guide](https://learn.unity.com/course/introduction-to-ui-in-unity/tutorial/optimizing-unity-ui)


## Metadata

- Original skill id: `unity-ui-composition`
- Skill version: `1.0.4`
- Platform: `codex`
- Source repository: `https://github.com/FunplayAI/funplay-unity-mcp`
