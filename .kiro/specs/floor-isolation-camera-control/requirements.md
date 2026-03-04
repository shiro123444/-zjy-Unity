# 需求文档 - 楼层隔离与摄像头控制

## 简介

本功能实现在设备列表面板中点击楼层标题时的视图隔离效果。当用户点击某个楼层时，场景中其他楼层（包括屋顶）将被隐藏，同时主摄像头将移动到预设的俯视位置，以便用户专注于查看选中的楼层。

## 术语表

- **Floor_Object**: 场景中代表单个楼层的GameObject，包含该楼层的所有3D模型和设备
- **Roof_Object**: 场景中代表屋顶的GameObject
- **Equipment_List_Panel**: 显示设备列表的UI面板组件（EquipmentListPanel.cs）
- **Floor_Header**: 设备列表面板中的楼层标题按钮
- **Main_Camera**: 场景中的主摄像头（Camera.main）
- **Isolation_Mode**: 楼层隔离模式，即只显示选中楼层而隐藏其他楼层的状态
- **Normal_Mode**: 正常模式，即显示所有楼层的状态
- **Camera_Controller**: 控制摄像头移动和旋转的组件

## 需求

### 需求 1: 楼层对象识别

**用户故事:** 作为系统，我需要识别场景中的所有楼层对象和屋顶对象，以便在隔离模式下控制它们的可见性。

#### 验收标准

1. THE Floor_Controller SHALL 在初始化时查找场景中所有名称包含"Floor"或"楼层"的GameObject
2. THE Floor_Controller SHALL 在初始化时查找场景中名称包含"Roof"或"屋顶"的GameObject
3. THE Floor_Controller SHALL 将找到的楼层对象存储在可访问的数据结构中
4. WHEN 场景中不存在任何楼层对象时，THE Floor_Controller SHALL 记录警告信息

### 需求 2: 楼层标题点击响应

**用户故事:** 作为用户，我想点击设备列表中的楼层标题，以便进入该楼层的隔离视图。

#### 验收标准

1. WHEN 用户点击设备列表中的任一楼层标题时，THE Equipment_List_Panel SHALL 通知Floor_Controller进入隔离模式
2. THE Equipment_List_Panel SHALL 将被点击的楼层标识传递给Floor_Controller
3. WHEN 楼层标题被点击时，THE Equipment_List_Panel SHALL 更新楼层标题的视觉状态以指示当前选中的楼层
4. THE Equipment_List_Panel SHALL 在楼层标题上添加退出隔离模式的交互提示

### 需求 3: 楼层可见性控制

**用户故事:** 作为用户，我想在点击楼层后只看到该楼层，以便专注于查看该楼层的设备和布局。

#### 验收标准

1. WHEN Floor_Controller进入隔离模式时，THE Floor_Controller SHALL 隐藏所有未被选中的楼层对象
2. WHEN Floor_Controller进入隔离模式时，THE Floor_Controller SHALL 隐藏Roof_Object
3. THE Floor_Controller SHALL 保持选中楼层对象的可见性
4. WHEN Floor_Controller退出隔离模式时，THE Floor_Controller SHALL 恢复所有楼层对象和Roof_Object的可见性
5. THE Floor_Controller SHALL 通过设置GameObject的active状态来控制可见性

### 需求 4: 摄像头位置控制

**用户故事:** 作为用户，我想在进入楼层隔离模式时摄像头自动移动到俯视位置，以便获得该楼层的最佳观察视角。

#### 验收标准

1. WHEN Floor_Controller进入隔离模式时，THE Camera_Controller SHALL 将Main_Camera的位置设置为x=-30, y=35, z=0
2. WHEN Floor_Controller进入隔离模式时，THE Camera_Controller SHALL 将Main_Camera的旋转设置为x=90, y=-90, z=0
3. THE Camera_Controller SHALL 在200毫秒内平滑过渡到目标位置和旋转
4. WHEN Floor_Controller退出隔离模式时，THE Camera_Controller SHALL 恢复Main_Camera到进入隔离模式前的位置和旋转
5. THE Camera_Controller SHALL 在摄像头移动期间禁用用户的摄像头控制输入

### 需求 5: 退出隔离模式

**用户故事:** 作为用户，我想能够退出楼层隔离模式，以便返回查看完整的建筑物。

#### 验收标准

1. WHEN 用户在隔离模式下再次点击当前选中的楼层标题时，THE Floor_Controller SHALL 退出隔离模式
2. WHEN 用户按下ESC键时，THE Floor_Controller SHALL 退出隔离模式
3. WHEN 用户点击设备列表面板外的空白区域时，THE Floor_Controller SHALL 退出隔离模式
4. THE Floor_Controller SHALL 在退出隔离模式时恢复所有楼层和屋顶的可见性
5. THE Floor_Controller SHALL 在退出隔离模式时恢复摄像头到之前的位置

### 需求 6: 隔离模式状态管理

**用户故事:** 作为系统，我需要维护隔离模式的状态，以便正确处理用户交互和视图切换。

#### 验收标准

1. THE Floor_Controller SHALL 维护一个布尔标志指示当前是否处于隔离模式
2. THE Floor_Controller SHALL 存储进入隔离模式前的摄像头位置和旋转
3. THE Floor_Controller SHALL 存储当前选中的楼层标识
4. WHEN 切换到不同楼层时，THE Floor_Controller SHALL 先退出当前隔离模式再进入新的隔离模式
5. THE Floor_Controller SHALL 在场景加载时初始化为Normal_Mode

### 需求 7: 与现有系统集成

**用户故事:** 作为开发者，我需要新功能与现有的设备列表和点击系统无缝集成，以便保持系统的一致性。

#### 验收标准

1. THE Floor_Controller SHALL 通过事件或回调与Equipment_List_Panel通信
2. WHEN 处于隔离模式时，THE ClickableObject SHALL 继续正常响应点击事件
3. THE Floor_Controller SHALL 不干扰InfoPanel的显示和隐藏逻辑
4. THE Floor_Controller SHALL 与现有的CameraController组件协同工作
5. WHEN 隔离模式激活时，THE Equipment_List_Panel SHALL 保持可见和可交互

### 需求 8: 视觉反馈

**用户故事:** 作为用户，我想获得清晰的视觉反馈，以便了解当前是否处于隔离模式以及哪个楼层被选中。

#### 验收标准

1. WHEN 进入隔离模式时，THE Equipment_List_Panel SHALL 高亮显示当前选中的楼层标题
2. WHEN 处于隔离模式时，THE Equipment_List_Panel SHALL 在选中的楼层标题上显示"退出隔离"或类似的提示文本
3. THE Equipment_List_Panel SHALL 使用不同的背景颜色区分隔离模式下的选中楼层标题
4. WHEN 退出隔离模式时，THE Equipment_List_Panel SHALL 移除所有隔离模式相关的视觉指示器
5. THE Equipment_List_Panel SHALL 在隔离模式下将未选中的楼层标题透明度降低到0.3

### 需求 9: 性能要求

**用户故事:** 作为用户，我期望楼层切换和摄像头移动流畅无卡顿，以便获得良好的使用体验。

#### 验收标准

1. THE Floor_Controller SHALL 在50毫秒内完成楼层可见性切换
2. THE Camera_Controller SHALL 使用平滑插值算法实现摄像头移动
3. THE Floor_Controller SHALL 在单帧内完成所有GameObject的active状态更新
4. WHEN 场景包含超过100个设备对象时，THE Floor_Controller SHALL 仍能在50毫秒内完成可见性切换
5. THE Camera_Controller SHALL 保持60帧每秒的帧率在摄像头移动期间

### 需求 10: 错误处理

**用户故事:** 作为系统，我需要妥善处理异常情况，以便在出现问题时不会导致系统崩溃或行为异常。

#### 验收标准

1. WHEN Main_Camera不存在时，THE Floor_Controller SHALL 记录错误信息并禁用摄像头控制功能
2. WHEN 请求的楼层对象不存在时，THE Floor_Controller SHALL 记录警告信息并保持当前状态
3. WHEN Equipment_List_Panel未正确初始化时，THE Floor_Controller SHALL 记录错误信息并使用默认配置
4. IF 摄像头移动过程中发生异常，THEN THE Camera_Controller SHALL 立即停止移动并恢复用户控制
5. THE Floor_Controller SHALL 在任何错误情况下都能通过ESC键退出隔离模式
