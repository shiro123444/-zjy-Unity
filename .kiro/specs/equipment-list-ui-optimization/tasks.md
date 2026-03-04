# Implementation Plan: Equipment List UI Optimization

## Overview

This implementation plan optimizes the equipment list UI layout by introducing configurable constants for floor header spacing and updating the CreateFloorHeader method to use these values. The changes will reduce the spacing between the expand/collapse arrow and title text from ~22 pixels to ~12-18 pixels, creating a more compact visual layout similar to Unity Editor's Hierarchy window.

## Tasks

- [ ] 1. Add layout configuration constants to EquipmentListPanel
  - Add four private const fields at class level: ARROW_HORIZONTAL_POSITION (5f), ARROW_SIZE (14f), TEXT_LEFT_OFFSET (22f), FLOOR_HEADER_HEIGHT (30f)
  - Include XML documentation comments explaining the purpose of each constant
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 2. Update CreateFloorHeader method to use configuration constants
  - [ ] 2.1 Replace hardcoded header height value
    - Replace `new Vector2(0, 30)` with `new Vector2(0, FLOOR_HEADER_HEIGHT)` in rect.sizeDelta assignment (line ~253)
    - _Requirements: 3.5_
  
  - [ ] 2.2 Replace hardcoded arrow position and size values
    - Replace `new Vector2(8, 0)` with `new Vector2(ARROW_HORIZONTAL_POSITION, 0)` in iconRect.anchoredPosition (line ~273)
    - Replace `new Vector2(15, 15)` with `new Vector2(ARROW_SIZE, ARROW_SIZE)` in iconRect.sizeDelta (line ~274)
    - _Requirements: 1.1, 1.2, 1.5_
  
  - [ ] 2.3 Replace hardcoded text offset value
    - Replace `new Vector2(30, 0)` with `new Vector2(TEXT_LEFT_OFFSET, 0)` in textRect.offsetMin (line ~303)
    - _Requirements: 1.3, 1.4_

- [ ] 3. Verify compilation and basic functionality
  - Build the project in Unity Editor to ensure no compilation errors
  - Open a scene with Equipment List Panel and verify floor headers render correctly
  - Test expand/collapse functionality to ensure it still works
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ]* 4. Create unit tests for configuration values
  - [ ]* 4.1 Create test file Tests/EditMode/EquipmentListPanelTests.cs
    - Set up NUnit test fixture with test GameObject hierarchy
    - Create helper methods for instantiating EquipmentListPanel in test mode
  
  - [ ]* 4.2 Write unit tests for configuration value ranges
    - Test ARROW_HORIZONTAL_POSITION is between 4 and 6 pixels
    - Test ARROW_SIZE is between 12 and 16 pixels
    - Test TEXT_LEFT_OFFSET is between 18 and 24 pixels
    - Test spacing gap (TEXT_LEFT_OFFSET - ARROW_HORIZONTAL_POSITION - ARROW_SIZE) is less than 8 pixels
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  
  - [ ]* 4.3 Write unit tests for specific layout verification
    - Test CreateFloorHeader sets correct arrow position
    - Test CreateFloorHeader sets correct arrow size
    - Test CreateFloorHeader sets correct text offset
    - Test floor header height is 30 pixels
    - _Requirements: 1.1, 1.2, 1.3, 3.5_

- [ ]* 5. Create property-based tests for universal properties
  - [ ]* 5.1 Set up FsCheck integration
    - Add FsCheck NuGet package to test assembly
    - Create test file Tests/EditMode/EquipmentListPanelPropertyTests.cs
    - Configure property tests to run 100 iterations minimum
  
  - [ ]* 5.2 Write property test for visual consistency
    - **Property 6: Arrow Visual Consistency**
    - **Validates: Requirements 3.1**
    - Test all floor headers have identical arrow font size and color
  
  - [ ]* 5.3 Write property test for spacing consistency
    - **Property 8: Spacing Consistency Across Headers**
    - **Validates: Requirements 3.3**
    - Test all floor headers have identical spacing between arrow and text
  
  - [ ]* 5.4 Write property test for toggle interaction
    - **Property 4: Floor Toggle Interaction**
    - **Validates: Requirements 2.1**
    - Test clicking floor header always toggles visibility state
  
  - [ ]* 5.5 Write property test for arrow icon state
    - **Property 5: Arrow Icon State Consistency**
    - **Validates: Requirements 2.2, 2.3**
    - Test arrow icon text matches floor expanded/collapsed state
  
  - [ ]* 5.6 Write property test for no overlap
    - **Property 13: No Overlap Between Arrow and Text**
    - **Validates: Requirements 4.3**
    - Test text left offset is always greater than arrow position plus arrow size

- [ ] 6. Manual testing and verification
  - Perform visual inspection in Unity Editor to verify spacing appears correct
  - Test button hover and press feedback
  - Test responsive behavior by resizing panel width
  - Test with Chinese font applied to verify proper rendering
  - _Requirements: 2.4, 3.4, 4.1, 4.2, 4.4_

- [ ] 7. Final checkpoint
  - Ensure all tests pass (if implemented)
  - Verify no compilation errors or warnings
  - Confirm all acceptance criteria are met
  - Ask the user if any adjustments are needed

## Notes

- Tasks marked with `*` are optional and can be skipped for faster implementation
- The core implementation (tasks 1-3) is minimal and focused on the essential changes
- Property-based tests provide comprehensive validation but require FsCheck setup
- Manual testing is important for visual verification that automated tests cannot fully cover
- All line numbers referenced are approximate and based on the current file structure
