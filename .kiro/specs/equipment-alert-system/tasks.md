# Implementation Plan: Equipment Alert System

## Overview

实现一个自动化设备警报系统，包括随机警报触发、红色波纹视觉效果、两阶段摄像头聚焦（楼层预设视角→设备聚焦视角）、楼层隔离显示和状态机管理。系统将与现有的FloorController、CameraController和SelectionRipple组件集成。

## Tasks

- [x] 1. 创建核心数据结构和枚举
  - 创建AlertState枚举（Idle, MovingToFloorView, WaitingAtFloorView, MovingToEquipmentView, FocusedOnEquipment）
  - 创建CameraView结构体（position, rotation）
  - 创建EquipmentInfo内部类（gameObject, floorName, isAlerting）
  - 创建FloorPresetView可序列化类（floorName, position, rotation）
  - _Requirements: 10.1, 7.1_

- [ ] 2. 创建AlertSystem MonoBehaviour基础框架
  - [x] 2.1 创建AlertSystem.cs脚本文件
    - 在Assets/Scripts/目录下创建AlertSystem.cs
    - 添加基础MonoBehaviour类结构
    - 添加所有公共配置字段（Alert Timing, Camera Transition, Ripple Effect, Equipment Identification, Floor Preset Views）
    - 添加私有状态字段（currentState, currentAlertEquipment, equipmentByFloor, allEquipment等）
    - _Requirements: 7.1, 7.2_
  
  - [ ]* 2.2 编写Property测试：配置参数边界验证
    - **Property 2: Alert Interval Bounds**
    - **Property 11: Focus Delay Bounds**
    - **Validates: Requirements 1.5, 4.2**

- [ ] 3. 实现设备发现和注册系统
  - [x] 3.1 实现DiscoverEquipment方法
    - 使用GameObject.FindGameObjectsWithTag查找所有设备
    - 遍历GameObject层级确定每个设备的父楼层
    - 将设备分组到equipmentByFloor字典
    - 记录无效设备（不在楼层下）并输出警告
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 1.1_
  
  - [x] 3.2 实现Initialize方法
    - 获取FloorController和CameraController引用
    - 验证依赖组件存在性
    - 调用DiscoverEquipment
    - 验证每个楼层至少有一个设备
    - 处理初始化错误（记录日志并禁用功能）
    - _Requirements: 7.2, 7.3, 7.4, 7.5_
  
  - [ ]* 3.3 编写Property测试：设备注册完整性
    - **Property 1: Equipment Registry Completeness**
    - **Property 9: Floor Identification**
    - **Property 26: Equipment Identification by Tag**
    - **Property 27: Equipment Grouping by Floor**
    - **Property 28: Invalid Hierarchy Handling**
    - **Validates: Requirements 1.1, 9.1, 9.2, 9.3, 9.4**

- [ ] 4. 实现状态机核心逻辑
  - [x] 4.1 实现TransitionToState方法
    - 验证状态转换的合法性（按照定义的序列）
    - 更新currentState
    - 记录状态转换日志
    - 拒绝非法转换并记录错误
    - _Requirements: 10.1, 10.2_
  
  - [x] 4.2 实现GetCurrentState和查询方法
    - 实现GetCurrentState()返回当前状态
    - 实现GetCurrentAlertEquipment()返回当前报警设备
    - _Requirements: 10.6_
  
  - [ ]* 4.3 编写Property测试：状态机转换验证
    - **Property 30: State Transition Sequence Enforcement**
    - **Property 31: Alert Triggering in Idle State**
    - **Property 32: Alert Queuing in Non-Idle State**
    - **Property 33: Return to Idle on Dismissal**
    - **Validates: Requirements 10.2, 10.3, 10.4, 10.5**

- [ ] 5. 实现警报触发和计时系统
  - [x] 5.1 实现TriggerRandomAlert方法
    - 从allEquipment列表随机选择一个设备
    - 验证只有一个设备处于警报状态
    - 设置设备的isAlerting标志
    - 调用StartAlertSequence
    - _Requirements: 1.2, 1.3, 1.4_
  
  - [x] 5.2 实现Update中的警报计时逻辑
    - 在Idle状态下递增alertTimer
    - 当达到nextAlertTime时触发TriggerRandomAlert
    - 使用Random.Range在minAlertInterval和maxAlertInterval之间生成下一个间隔
    - 验证配置参数在有效范围内（5-300秒）
    - _Requirements: 1.5, 6.6_
  
  - [x] 5.3 实现StartAlertSystem和StopAlertSystem方法
    - StartAlertSystem设置isSystemActive为true并初始化计时器
    - StopAlertSystem设置isSystemActive为false
    - 在Start方法中调用Initialize和StartAlertSystem
    - _Requirements: 7.6_
  
  - [ ]* 5.4 编写Property测试：警报触发逻辑
    - **Property 3: Single Active Alert Invariant**
    - **Property 4: Alert State Activation**
    - **Property 19: Timer Reset on Dismissal**
    - **Validates: Requirements 1.2, 1.3, 1.4, 6.6**

- [ ] 6. 实现波纹效果管理
  - [x] 6.1 实现CreateRippleEffect方法
    - 在设备位置创建新的GameObject
    - 添加SelectionRipple组件
    - 使用配置的颜色、大小和透明度初始化波纹
    - 将波纹GameObject保存到currentRippleObject
    - 设置波纹位置为设备的中心点
    - _Requirements: 2.1, 2.2, 2.4, 2.5_
  
  - [x] 6.2 实现DestroyRippleEffect方法
    - 销毁currentRippleObject
    - 清空currentRippleObject引用
    - _Requirements: 2.6_
  
  - [ ]* 6.3 编写Property测试：波纹效果生命周期
    - **Property 5: Ripple Creation on Alert**
    - **Property 6: Ripple Color Configuration**
    - **Property 7: Ripple Position Alignment**
    - **Property 8: Ripple Cleanup on Dismissal**
    - **Validates: Requirements 2.1, 2.2, 2.4, 2.6**

- [ ] 7. 实现摄像头视角计算
  - [x] 7.1 实现CalculateFloorPresetView方法
    - 首先检查floorPresetViews列表中是否有配置的视角
    - 如果有配置，返回配置的CameraView
    - 如果没有配置，计算楼层的边界（Bounds）
    - 基于楼层边界计算默认视角（位置和旋转）
    - _Requirements: 3.2, 3.5_
  
  - [x] 7.2 实现CalculateEquipmentFocusView方法
    - 获取设备的位置和边界
    - 计算摄像头位置（距离设备equipmentFocusDistance）
    - 计算摄像头旋转（朝向设备）
    - 确保设备在摄像头视锥内可见
    - 返回CameraView结构
    - _Requirements: 4.3, 4.6_
  
  - [ ]* 7.3 编写Property测试：摄像头视角计算
    - **Property 12: Equipment Focus View Calculation**
    - **Validates: Requirements 4.6**

- [ ] 8. 实现摄像头过渡控制
  - [x] 8.1 实现MoveCameraToView协程
    - 保存当前摄像头位置和旋转到savedCameraView
    - 禁用CameraController组件
    - 使用Lerp和Slerp平滑过渡到目标视角
    - 在cameraTransitionTime内完成过渡
    - 设置isCameraTransitioning标志
    - _Requirements: 3.4, 4.5, 8.1, 8.2_
  
  - [x] 8.2 实现摄像头控制状态管理
    - 在警报开始时禁用CameraController
    - 在警报结束时重新启用CameraController
    - 确保在Idle状态下CameraController保持启用
    - _Requirements: 8.3, 8.4_
  
  - [ ]* 8.3 编写Property测试：摄像头控制集成
    - **Property 10: Camera Transition Timing**
    - **Property 22: Camera Control Disabled During Movement**
    - **Property 23: Camera State Preservation**
    - **Property 24: Camera Control Non-Interference in Idle**
    - **Property 25: Programmatic Camera Control**
    - **Validates: Requirements 3.4, 4.5, 8.1, 8.2, 8.3, 8.4, 8.5**

- [ ] 9. 实现楼层隔离控制
  - [x] 9.1 实现楼层隔离激活逻辑
    - 在开始移动到楼层视角时调用FloorController.EnterIsolationMode
    - 传递目标楼层名称
    - 验证FloorController进入隔离模式
    - _Requirements: 5.1, 5.2, 5.3_
  
  - [x] 9.2 实现楼层隔离退出逻辑
    - 在DismissCurrentAlert中调用FloorController.ExitIsolationMode
    - 验证所有楼层和屋顶恢复可见
    - _Requirements: 5.5, 5.6_
  
  - [ ]* 9.3 编写Property测试：楼层隔离行为
    - **Property 13: Floor Isolation Activation**
    - **Property 14: Floor Visibility in Isolation**
    - **Property 15: Isolation Mode Persistence**
    - **Property 16: Isolation Mode Exit on Dismissal**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5, 5.6**

- [ ] 10. 实现完整警报序列
  - [x] 10.1 实现StartAlertSequence方法
    - 转换状态到MovingToFloorView
    - 识别设备所属楼层
    - 创建波纹效果
    - 激活楼层隔离模式
    - 计算楼层预设视角
    - 启动摄像头过渡到楼层视角
    - 在过渡完成后转换到WaitingAtFloorView状态
    - _Requirements: 1.3, 2.1, 3.1, 3.2, 3.3, 5.1_
  
  - [x] 10.2 实现楼层视角等待逻辑
    - 在WaitingAtFloorView状态下递增focusDelayTimer
    - 当达到focusDelayDuration时转换到MovingToEquipmentView
    - 验证focusDelayDuration在有效范围内（1-10秒）
    - _Requirements: 4.1, 4.2_
  
  - [x] 10.3 实现设备聚焦过渡
    - 计算设备聚焦视角
    - 启动摄像头过渡到设备视角
    - 在过渡完成后转换到FocusedOnEquipment状态
    - _Requirements: 4.3, 4.4, 4.5_

- [ ] 11. 实现警报解除功能
  - [~] 11.1 实现DismissCurrentAlert方法
    - 验证当前有活动警报
    - 设置设备的isAlerting为false
    - 销毁波纹效果
    - 退出楼层隔离模式
    - 重新启用CameraController
    - 重置警报计时器
    - 转换状态到Idle
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  
  - [ ]* 11.2 编写Property测试：警报解除流程
    - **Property 17: Alert State Deactivation on Dismissal**
    - **Property 18: Camera Control Restoration**
    - **Validates: Requirements 6.2, 6.5**

- [ ] 12. 实现手动触发和调试功能
  - [x] 12.1 实现TriggerAlertManually方法
    - 接受GameObject参数
    - 验证设备在注册表中
    - 验证系统在Idle状态
    - 调用StartAlertSequence
    - _Requirements: 7.6_
  
  - [x] 12.2 添加调试日志和状态查询
    - 在关键状态转换点添加Debug.Log
    - 实现状态查询方法用于调试
    - 添加配置参数验证日志
    - _Requirements: 10.6_

- [ ] 13. 实现错误处理和恢复机制
  - [x] 13.1 实现初始化错误处理
    - 处理缺失依赖（FloorController, CameraController）
    - 处理无设备场景
    - 处理设备层级错误
    - 记录错误并禁用功能
    - _Requirements: 7.5, 9.4_
  
  - [x] 13.2 实现运行时错误处理
    - 处理设备在警报期间被销毁
    - 处理摄像头过渡中断
    - 处理FloorController状态不匹配
    - 实现状态超时和强制恢复
    - _Requirements: 设计文档 Error Handling 章节_
  
  - [x] 13.3 实现配置参数验证
    - 验证并限制alertInterval在[5, 300]范围
    - 验证并限制focusDelay在[1, 10]范围
    - 验证cameraTransitionTime > 0
    - 使用Mathf.Clamp并记录警告
    - _Requirements: 1.5, 4.2_

- [ ] 14. 实现动态设备管理（可选扩展）
  - [x] 14.1 添加运行时设备注册方法
    - 实现RegisterEquipment(GameObject)方法
    - 实现UnregisterEquipment(GameObject)方法
    - 更新equipmentByFloor和allEquipment
    - _Requirements: 9.5_
  
  - [ ]* 14.2 编写Property测试：动态设备管理
    - **Property 29: Dynamic Equipment Registry Updates**
    - **Validates: Requirements 9.5**

- [ ] 15. 集成测试和最终验证
  - [x] 15.1 创建测试场景
    - 设置包含多个楼层和设备的测试场景
    - 配置FloorController和CameraController
    - 添加AlertSystem组件并配置参数
    - 标记设备GameObject（使用"Equipment"标签）
  
  - [x] 15.2 测试完整警报流程
    - 启动系统并验证自动触发
    - 验证摄像头两阶段过渡
    - 验证楼层隔离和波纹效果
    - 测试手动解除警报
    - 验证系统返回Idle状态
  
  - [ ]* 15.3 编写集成测试
    - 测试完整警报序列（从触发到解除）
    - 测试多次连续警报
    - 测试用户交互（手动解除）
    - 测试性能（大量设备场景）

- [x] 16. Checkpoint - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户

## Notes

- 任务标记`*`的为可选任务，可以跳过以加快MVP开发
- 每个任务都引用了具体的需求编号以便追溯
- Checkpoint任务确保增量验证
- Property测试验证通用正确性属性
- 集成测试验证与Unity组件的实际交互
- 系统设计为单一MonoBehaviour管理器，简化集成
- 复用现有的FloorController、CameraController和SelectionRipple组件
