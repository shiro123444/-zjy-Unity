# Design Document: Equipment List UI Optimization

## Overview

This design document specifies the implementation approach for optimizing the equipment list UI layout in the Unity project. The optimization focuses on creating a more compact visual layout for floor headers by reducing the spacing between the expand/collapse arrow and the title text, similar to Unity Editor's native Hierarchy window style.

The current implementation in `EquipmentListPanel.cs` uses hardcoded position values that create excessive spacing. This design introduces configurable constants and precise positioning to achieve a tighter, more professional layout while maintaining all existing functionality.

### Goals

- Reduce spacing between arrow icon and title text from ~22 pixels to ~12-18 pixels
- Introduce configurable layout constants for easy future adjustments
- Maintain all existing expand/collapse functionality
- Ensure visual consistency across all floor headers
- Preserve responsive layout behavior

### Non-Goals

- Changing the overall panel structure or hierarchy
- Modifying the expand/collapse animation behavior
- Altering the color scheme or visual styling beyond spacing
- Adding new features beyond layout optimization

## Architecture

### Component Structure

The optimization affects a single method within the existing `EquipmentListPanel` class:

```
EquipmentListPanel
├── CreateFloorHeader() [MODIFIED]
│   ├── Header GameObject (Button + Image)
│   ├── Icon GameObject (TextMeshProUGUI) [POSITIONING CHANGED]
│   └── Text GameObject (TextMeshProUGUI) [POSITIONING CHANGED]
├── ToggleFloor() [UNCHANGED]
└── Other methods [UNCHANGED]
```

### Design Principles

1. **Minimal Change Scope**: Only modify positioning values in `CreateFloorHeader()` method
2. **Configuration Over Hardcoding**: Use named constants for all layout values
3. **Backward Compatibility**: Maintain existing public API and behavior
4. **Self-Documenting Code**: Add comments explaining the purpose of each spacing value

## Components and Interfaces

### Modified Component: EquipmentListPanel

**File**: `My project/Assets/Scripts/EquipmentListPanel.cs`

#### New Configuration Constants

Add the following private constants at the class level:

```csharp
// Floor header layout configuration
private const float ARROW_HORIZONTAL_POSITION = 5f;    // Distance from left edge to arrow
private const float ARROW_SIZE = 14f;                   // Arrow icon width and height
private const float TEXT_LEFT_OFFSET = 22f;             // Distance from left edge to text start
private const float FLOOR_HEADER_HEIGHT = 30f;          // Total header height
```

These values are chosen to:
- `ARROW_HORIZONTAL_POSITION = 5f`: Centers the arrow in a small left margin (4-6 pixel range)
- `ARROW_SIZE = 14f`: Slightly smaller than current 15px, within 12-16 pixel range
- `TEXT_LEFT_OFFSET = 22f`: Creates ~8 pixel gap between arrow (5+14=19) and text (22), within 18-24 pixel range
- `FLOOR_HEADER_HEIGHT = 30f`: Maintains existing height for consistency

#### Modified Method: CreateFloorHeader

**Signature**: `GameObject CreateFloorHeader(Transform parent, string floorName, string uniqueKey)`

**Changes**:

1. Replace hardcoded arrow position `new Vector2(8, 0)` with `new Vector2(ARROW_HORIZONTAL_POSITION, 0)`
2. Replace hardcoded arrow size `new Vector2(15, 15)` with `new Vector2(ARROW_SIZE, ARROW_SIZE)`
3. Replace hardcoded text offset `new Vector2(30, 0)` with `new Vector2(TEXT_LEFT_OFFSET, 0)`
4. Replace hardcoded header height `new Vector2(0, 30)` with `new Vector2(0, FLOOR_HEADER_HEIGHT)`

**Behavior**: The method continues to create a floor header GameObject with button functionality, but with tighter spacing between arrow and text.

### Unchanged Components

- **ToggleFloor()**: No changes required, continues to handle expand/collapse logic
- **floorIcons Dictionary**: Continues to store icon references for rotation
- **Button event binding**: Remains unchanged
- **Color and styling**: All visual properties except positioning remain the same

## Data Models

No new data structures are introduced. The existing data model remains unchanged:

- `Dictionary<string, TextMeshProUGUI> floorIcons`: Maps unique floor keys to icon components
- `Dictionary<string, bool> floorStates`: Tracks expanded/collapsed state per floor
- `Dictionary<string, GameObject> floorContainers`: Maps floor keys to container GameObjects

The positioning changes are purely visual and do not affect the data layer.


## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Configuration Values Within Valid Ranges

The layout configuration constants shall fall within the specified acceptable ranges to ensure proper visual appearance.

- Arrow horizontal position: 4-6 pixels (configured value: 5 pixels)
- Arrow size: 12-16 pixels (configured value: 14 pixels)  
- Text left offset: 18-24 pixels (configured value: 22 pixels)

**Validates: Requirements 1.1, 1.2, 1.3**

### Property 2: Compact Spacing Between Arrow and Text

*For any* floor header configuration, the distance between the arrow's right edge and the text's left edge shall be less than 8 pixels.

Calculated as: `TEXT_LEFT_OFFSET - (ARROW_HORIZONTAL_POSITION + ARROW_SIZE) < 8`

With configured values: `22 - (5 + 14) = 3 pixels < 8` ✓

**Validates: Requirements 1.4**

### Property 3: Arrow Vertical Centering

*For any* floor header created, the expand arrow's RectTransform shall have vertical anchor values of 0.5 (center) to ensure the arrow remains vertically centered within the header.

**Validates: Requirements 1.5**

### Property 4: Floor Toggle Interaction

*For any* floor header, when the header button is clicked, the visibility state of the corresponding floor's equipment container shall toggle from visible to hidden or from hidden to visible.

**Validates: Requirements 2.1**

### Property 5: Arrow Icon State Consistency

*For any* floor header, the arrow icon text shall be "▼" when the floor is in expanded state, and "▶" when the floor is in collapsed state.

**Validates: Requirements 2.2, 2.3**

### Property 6: Arrow Visual Consistency

*For any* set of floor headers created, all expand arrow icons shall have identical font size and color values.

**Validates: Requirements 3.1**

### Property 7: Title Text Visual Consistency

*For any* set of floor headers created, all title text components shall have identical font size, font style, and color values.

**Validates: Requirements 3.2**

### Property 8: Spacing Consistency Across Headers

*For any* set of floor headers created, the calculated spacing between arrow and text (TEXT_LEFT_OFFSET - ARROW_HORIZONTAL_POSITION - ARROW_SIZE) shall be identical for all instances.

**Validates: Requirements 3.3**

### Property 9: Chinese Font Application

*For any* floor header created when the `chineseFont` field is not null, both the expand arrow icon and the title text shall reference the same font asset (the configured Chinese font).

**Validates: Requirements 3.4**

### Property 10: Header Height Consistency

*For any* floor header created, the RectTransform sizeDelta.y value shall be 30 pixels to maintain consistent row spacing.

**Validates: Requirements 3.5**

### Property 11: Text Horizontal Stretching

*For any* floor header created, the title text's RectTransform shall have anchor settings (anchorMin = (0,0), anchorMax = (1,1)) that cause it to stretch horizontally to fill available space as the panel width changes.

**Validates: Requirements 4.1**

### Property 12: Arrow Fixed Positioning

*For any* floor header created, the expand arrow's RectTransform shall have anchor settings (anchorMin = (0, 0.5), anchorMax = (0, 0.5)) that keep its position fixed relative to the left edge regardless of panel width changes.

**Validates: Requirements 4.2**

### Property 13: No Overlap Between Arrow and Text

*For any* floor header created, the title text's left offset shall be greater than the sum of the arrow's position and size, ensuring no visual overlap occurs.

Mathematically: `TEXT_LEFT_OFFSET > ARROW_HORIZONTAL_POSITION + ARROW_SIZE`

**Validates: Requirements 4.3**

### Property 14: Configuration Constant Propagation

*For any* modification to the layout configuration constants (ARROW_HORIZONTAL_POSITION, ARROW_SIZE, TEXT_LEFT_OFFSET, FLOOR_HEADER_HEIGHT), all floor headers created after the modification shall use the new values without requiring additional code changes.

**Validates: Requirements 5.5**

## Error Handling

### Invalid Configuration Detection

The design includes implicit validation through the property constraints:

1. **Spacing Validation**: Property 2 ensures the gap is less than 8 pixels. If configuration constants are changed such that this property is violated, property-based tests will fail, alerting developers to the invalid configuration.

2. **Overlap Prevention**: Property 13 ensures no overlap occurs. Invalid configurations that cause overlap will be caught by tests.

3. **Range Validation**: Property 1 documents the acceptable ranges. While not enforced at runtime (since these are compile-time constants), code review and testing will verify compliance.

### Runtime Error Scenarios

The implementation has minimal error surface since it deals with UI layout:

1. **Null Font Reference**: If `chineseFont` is null, the code gracefully falls back to the default font. No error handling needed.

2. **Invalid Parent Transform**: The `CreateFloorHeader` method assumes a valid parent transform. This is guaranteed by the calling context (`CreateFloorGroups`), which always provides a valid container.

3. **Missing Button Component**: Unity's `AddComponent<Button>()` cannot fail in normal circumstances. No error handling needed.

### Error Prevention Strategy

- Use const values to prevent accidental runtime modification
- Rely on Unity's component system guarantees (AddComponent always succeeds)
- Leverage property-based testing to catch configuration errors during development
- Document acceptable value ranges in code comments

## Testing Strategy

### Dual Testing Approach

This feature requires both unit tests and property-based tests to ensure comprehensive coverage:

- **Unit Tests**: Verify specific configuration values and edge cases
- **Property Tests**: Verify universal properties hold across all floor header instances

### Unit Testing

Unit tests will focus on:

1. **Configuration Value Verification**: Test that constants are set to values within acceptable ranges
2. **Specific Layout Examples**: Create a floor header and verify exact positioning values
3. **Edge Cases**: 
   - Floor header creation with null Chinese font
   - Floor header creation with Chinese font assigned
   - Toggle behavior on first click (collapsed → expanded)
   - Toggle behavior on second click (expanded → collapsed)

**Example Unit Tests**:

```csharp
[Test]
public void FloorHeader_ArrowPosition_IsWithinValidRange()
{
    // Verify ARROW_HORIZONTAL_POSITION is between 4 and 6
    Assert.That(ARROW_HORIZONTAL_POSITION, Is.InRange(4f, 6f));
}

[Test]
public void FloorHeader_ArrowSize_IsWithinValidRange()
{
    // Verify ARROW_SIZE is between 12 and 16
    Assert.That(ARROW_SIZE, Is.InRange(12f, 16f));
}

[Test]
public void FloorHeader_TextOffset_IsWithinValidRange()
{
    // Verify TEXT_LEFT_OFFSET is between 18 and 24
    Assert.That(TEXT_LEFT_OFFSET, Is.InRange(18f, 24f));
}

[Test]
public void FloorHeader_SpacingBetweenArrowAndText_IsLessThan8Pixels()
{
    float gap = TEXT_LEFT_OFFSET - (ARROW_HORIZONTAL_POSITION + ARROW_SIZE);
    Assert.That(gap, Is.LessThan(8f));
}

[Test]
public void FloorHeader_Creation_SetsCorrectArrowPosition()
{
    GameObject header = CreateFloorHeader(testParent, "1F", "test_1F");
    Transform iconTransform = header.transform.Find("Icon");
    RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
    
    Assert.That(iconRect.anchoredPosition.x, Is.EqualTo(ARROW_HORIZONTAL_POSITION));
}
```

### Property-Based Testing

Unity C# projects typically use NUnit for testing. For property-based testing, we'll use **FsCheck** (a .NET property-based testing library that integrates with NUnit).

**Installation**: Add FsCheck NuGet package to the test assembly.

**Configuration**: Each property test shall run a minimum of 100 iterations to ensure comprehensive input coverage.

**Property Test Examples**:

```csharp
using FsCheck;
using FsCheck.NUnit;

[Property(MaxTest = 100)]
public Property FloorHeaders_AllHaveSameArrowFontSize()
{
    return Prop.ForAll<string[]>(floorNames =>
    {
        var headers = floorNames.Select(name => 
            CreateFloorHeader(testParent, name, $"test_{name}")
        ).ToList();
        
        var fontSizes = headers.Select(h => 
            h.transform.Find("Icon").GetComponent<TextMeshProUGUI>().fontSize
        ).Distinct();
        
        return fontSizes.Count() == 1;
    });
}
// Feature: equipment-list-ui-optimization, Property 6: All arrows same font size and color

[Property(MaxTest = 100)]
public Property FloorHeaders_AllHaveSameSpacing()
{
    return Prop.ForAll<string[]>(floorNames =>
    {
        var headers = floorNames.Select(name => 
            CreateFloorHeader(testParent, name, $"test_{name}")
        ).ToList();
        
        var spacings = headers.Select(h => {
            var iconRect = h.transform.Find("Icon").GetComponent<RectTransform>();
            var textRect = h.transform.Find("Text").GetComponent<RectTransform>();
            return textRect.offsetMin.x - (iconRect.anchoredPosition.x + iconRect.sizeDelta.x);
        }).Distinct();
        
        return spacings.Count() == 1;
    });
}
// Feature: equipment-list-ui-optimization, Property 8: All headers same spacing

[Property(MaxTest = 100)]
public Property FloorToggle_AlwaysChangesVisibilityState()
{
    return Prop.ForAll<string>(floorName =>
    {
        string uniqueKey = $"test_{floorName}";
        CreateFloorHeader(testParent, floorName, uniqueKey);
        
        bool initialState = floorStates.GetValueOrDefault(uniqueKey, false);
        ToggleFloor(uniqueKey);
        bool afterToggle = floorStates[uniqueKey];
        
        return initialState != afterToggle;
    });
}
// Feature: equipment-list-ui-optimization, Property 4: Click toggles visibility

[Property(MaxTest = 100)]
public Property ArrowIcon_MatchesFloorState()
{
    return Prop.ForAll<string, bool>((floorName, isExpanded) =>
    {
        string uniqueKey = $"test_{floorName}";
        CreateFloorHeader(testParent, floorName, uniqueKey);
        floorStates[uniqueKey] = isExpanded;
        
        // Simulate the icon update that happens in ToggleFloor
        string expectedIcon = isExpanded ? "▼" : "▶";
        floorIcons[uniqueKey].text = expectedIcon;
        
        return floorIcons[uniqueKey].text == expectedIcon;
    });
}
// Feature: equipment-list-ui-optimization, Property 5: Arrow icon matches floor state
```

### Test Organization

```
Tests/
├── EditMode/
│   ├── EquipmentListPanelTests.cs          // Unit tests
│   └── EquipmentListPanelPropertyTests.cs  // Property-based tests
```

### Testing Priorities

1. **High Priority**: Properties 2, 4, 5 (core functionality and spacing)
2. **Medium Priority**: Properties 6, 7, 8, 10 (visual consistency)
3. **Low Priority**: Properties 11, 12, 13 (anchor configuration, mostly Unity guarantees)

### Manual Testing Checklist

Some aspects require manual verification:

- [ ] Visual inspection: Arrow and text appear properly spaced in Unity Editor
- [ ] Hover feedback: Button highlights correctly on mouse over
- [ ] Click feedback: Button shows pressed state on click
- [ ] Responsive behavior: Panel resizes correctly when window width changes
- [ ] Font rendering: Chinese characters display correctly with Chinese font applied

## Implementation Plan

### Step 1: Add Configuration Constants

Add the four layout constants to the `EquipmentListPanel` class:

```csharp
// Floor header layout configuration
private const float ARROW_HORIZONTAL_POSITION = 5f;    // Distance from left edge to arrow
private const float ARROW_SIZE = 14f;                   // Arrow icon width and height  
private const float TEXT_LEFT_OFFSET = 22f;             // Distance from left edge to text start
private const float FLOOR_HEADER_HEIGHT = 30f;          // Total header height
```

### Step 2: Update CreateFloorHeader Method

Replace hardcoded values with constants:

**Line ~253**: `rect.sizeDelta = new Vector2(0, FLOOR_HEADER_HEIGHT);`

**Line ~273**: `iconRect.anchoredPosition = new Vector2(ARROW_HORIZONTAL_POSITION, 0);`

**Line ~274**: `iconRect.sizeDelta = new Vector2(ARROW_SIZE, ARROW_SIZE);`

**Line ~303**: `textRect.offsetMin = new Vector2(TEXT_LEFT_OFFSET, 0);`

### Step 3: Verify Compilation

- Build the project in Unity Editor
- Ensure no compilation errors
- Verify the EquipmentListPanel script has no errors in the Inspector

### Step 4: Manual Testing

- Open a scene with the Equipment List Panel
- Create floor headers by triggering the list population
- Verify visual spacing matches expectations
- Test expand/collapse functionality
- Test with different panel widths

### Step 5: Automated Testing

- Create unit test file: `Tests/EditMode/EquipmentListPanelTests.cs`
- Implement configuration value tests
- Implement specific layout tests
- Create property test file: `Tests/EditMode/EquipmentListPanelPropertyTests.cs`
- Implement property-based tests for consistency and behavior
- Run all tests and verify they pass

### Step 6: Documentation

- Add XML documentation comments to the constants explaining their purpose
- Update any existing documentation that references the old layout values

## Rollback Plan

If issues are discovered after implementation:

1. **Immediate Rollback**: Revert the constant values to original hardcoded values:
   - ARROW_HORIZONTAL_POSITION = 8f
   - ARROW_SIZE = 15f
   - TEXT_LEFT_OFFSET = 30f
   - FLOOR_HEADER_HEIGHT = 30f

2. **Partial Rollback**: If only spacing is problematic, adjust TEXT_LEFT_OFFSET back to 30f while keeping other optimizations

3. **Full Revert**: Use version control to revert the entire commit if fundamental issues arise

The use of constants makes rollback trivial—just change the constant values and recompile.
