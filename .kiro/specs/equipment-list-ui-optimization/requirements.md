# Requirements Document

## Introduction

本文档定义了设备列表UI优化功能的需求。该功能旨在改进Unity项目中设备列表面板的展开/折叠箭头与标题文字的布局，使其更加紧凑，类似Unity Editor原生的Hierarchy窗口样式。

当前实现中，展开箭头（▶）与楼层标题文字之间存在较大间距，影响了UI的紧凑性和视觉一致性。优化后的布局将使箭头和文字更加贴近，提升用户体验。

## Glossary

- **Equipment_List_Panel**: 设备列表面板，显示场景中所有可点击设备的UI组件
- **Floor_Header**: 楼层分组标题，包含展开/折叠箭头和楼层名称的UI元素
- **Expand_Arrow**: 展开/折叠箭头，使用Unicode字符"▶"表示，点击可切换楼层内容的显示状态
- **Title_Text**: 楼层标题文字，显示楼层名称（如"1楼"、"2楼"等）
- **RectTransform**: Unity UI组件，用于控制UI元素的位置、大小和锚点
- **TextMeshProUGUI**: TextMeshPro的UI文本组件，用于渲染高质量文本

## Requirements

### Requirement 1: 紧凑的箭头布局

**User Story:** 作为用户，我希望展开箭头与标题文字之间的间距更小，以便获得更紧凑的视觉效果，类似Unity Editor的Hierarchy窗口。

#### Acceptance Criteria

1. THE Expand_Arrow SHALL have a horizontal position between 4 and 6 pixels from the left edge of the Floor_Header
2. THE Expand_Arrow SHALL have a size between 12 and 16 pixels for both width and height
3. THE Title_Text SHALL have a left offset between 18 and 24 pixels from the left edge of the Floor_Header
4. WHEN the Floor_Header is rendered, THE distance between the Expand_Arrow right edge and the Title_Text left edge SHALL be less than 8 pixels
5. THE Expand_Arrow SHALL remain vertically centered within the Floor_Header

### Requirement 2: 保持现有交互功能

**User Story:** 作为用户，我希望在优化布局后，展开/折叠功能仍然正常工作，以便继续使用设备列表的分组功能。

#### Acceptance Criteria

1. WHEN the user clicks on the Floor_Header, THE Equipment_List_Panel SHALL toggle the visibility of equipment items under that floor
2. WHEN a floor is expanded, THE Expand_Arrow SHALL rotate to point downward (▼)
3. WHEN a floor is collapsed, THE Expand_Arrow SHALL point to the right (▶)
4. THE Floor_Header button hover and press visual feedback SHALL remain functional
5. FOR ALL floor toggle operations, the layout refresh SHALL complete within one frame

### Requirement 3: 视觉一致性

**User Story:** 作为用户，我希望优化后的UI在不同楼层标题上保持一致的视觉效果，以便获得统一的用户体验。

#### Acceptance Criteria

1. THE Expand_Arrow SHALL use the same font size and color across all Floor_Header instances
2. THE Title_Text SHALL use the same font size, style, and color across all Floor_Header instances
3. THE spacing between Expand_Arrow and Title_Text SHALL be identical for all Floor_Header instances
4. WHEN the Chinese font is applied, THE Expand_Arrow and Title_Text SHALL both use the same font asset
5. THE Floor_Header height SHALL remain at 30 pixels to maintain consistent row spacing

### Requirement 4: 响应式布局适配

**User Story:** 作为用户，我希望优化后的布局能够适应不同的面板宽度，以便在调整窗口大小时保持良好的显示效果。

#### Acceptance Criteria

1. WHEN the Equipment_List_Panel width changes, THE Title_Text SHALL stretch horizontally to fill available space
2. THE Expand_Arrow position SHALL remain fixed relative to the left edge regardless of panel width
3. THE Title_Text SHALL not overlap with the Expand_Arrow at any panel width
4. WHEN the panel width is less than 200 pixels, THE Title_Text SHALL truncate with ellipsis if necessary
5. THE RectTransform anchor settings SHALL ensure proper scaling behavior

### Requirement 5: 代码可维护性

**User Story:** 作为开发者，我希望布局参数易于调整，以便未来可以快速微调间距和尺寸。

#### Acceptance Criteria

1. THE CreateFloorHeader method SHALL use named constants or configurable fields for arrow position values
2. THE CreateFloorHeader method SHALL use named constants or configurable fields for arrow size values
3. THE CreateFloorHeader method SHALL use named constants or configurable fields for text offset values
4. THE spacing values SHALL be documented with comments explaining their purpose
5. WHEN a spacing constant is modified, THE change SHALL apply to all Floor_Header instances without additional code changes
