// ed8728fa
ldsfld VRageMath.Vector2 VRageMath.Vector2::Zero
stloc.0
ldc.i4.8
ldc.i4.8
ldc.i4.8
call static VRageMath.Vector2 Sandbox.Graphics.MyGuiManager::ComputeFullscreenGuiCoordinate(VRage.Utils.MyGuiDrawAlignEnum align, System.Int32 pixelOffsetX, System.Int32 pixelOffsetY)
stloc.1
call static System.String Sandbox.Engine.Networking.MyGameService::get_BranchName()
call static System.Boolean System.String::IsNullOrEmpty(System.String value)
brtrue L0
ldsfld System.Text.StringBuilder Sandbox.Game.Screens.MyGuiScreenMainMenuBase::BranchName
callvirt System.Text.StringBuilder System.Text.StringBuilder::Clear()
pop
ldsfld System.Text.StringBuilder Sandbox.Game.Screens.MyGuiScreenMainMenuBase::BranchName
ldstr " "
callvirt System.Text.StringBuilder System.Text.StringBuilder::Append(System.String value)
pop
ldsfld System.Text.StringBuilder Sandbox.Game.Screens.MyGuiScreenMainMenuBase::BranchName
call static System.String Sandbox.Engine.Networking.MyGameService::get_BranchName()
callvirt System.Text.StringBuilder System.Text.StringBuilder::Append(System.String value)
pop
ldstr "BuildInfoHighlight"
ldsfld System.Text.StringBuilder Sandbox.Game.Screens.MyGuiScreenMainMenuBase::BranchName
ldc.r4 0.6
call static VRageMath.Vector2 Sandbox.Graphics.MyGuiManager::MeasureString(System.String font, System.Text.StringBuilder text, System.Single scale)
stloc.0
ldstr "BuildInfoHighlight"
ldsfld System.Text.StringBuilder Sandbox.Game.Screens.MyGuiScreenMainMenuBase::BranchName
callvirt virtual System.String System.Object::ToString()
ldloc.1
ldc.r4 0.6
ldsfld VRageMath.Vector4 Sandbox.Graphics.GUI.MyGuiConstants::LABEL_TEXT_COLOR
ldarg.0
ldfld System.Single Sandbox.Graphics.GUI.MyGuiScreenBase::m_transitionAlpha
call static VRageMath.Vector4 VRageMath.Vector4::op_Multiply(VRageMath.Vector4 value1, System.Single scaleFactor)
call static VRageMath.Color VRageMath.Color::op_Implicit(VRageMath.Vector4 v)
ldc.r4 0.6
newobj System.Void VRageMath.Color::.ctor(VRageMath.Color color, System.Single a)
newobj System.Void System.Nullable`1<VRageMath.Color>::.ctor(VRageMath.Color value)
ldc.i4.8
ldc.i4.0
ldc.r4 Infinity
ldc.i4.0
call static System.Void Sandbox.Graphics.MyGuiManager::DrawString(System.String font, System.String text, VRageMath.Vector2 normalizedCoord, System.Single scale, System.Nullable`1<VRageMath.Color> colorMask, VRage.Utils.MyGuiDrawAlignEnum drawAlign, System.Boolean useFullClientArea, System.Single maxTextWidth, System.Boolean ignoreBounds)
ldloca.s 1 (VRageMath.Vector2)
ldflda System.Single VRageMath.Vector2::X
dup
ldind.r4
ldloc.0
ldfld System.Single VRageMath.Vector2::X
sub
stind.r4
L0:
call static System.Text.StringBuilder VRage.Game.MyFinalBuildConstants::get_APP_VERSION_STRING_DOTS()
callvirt virtual System.String System.Object::ToString()
ldstr " b{0}"
ldsflda Sandbox.Game.MyBasicGameInfo Sandbox.Game.MyPerGameSettings::BasicGameInfo
ldfld System.Nullable`1<System.Int32> Sandbox.Game.MyBasicGameInfo::ClientBuildNumber
box System.Nullable`1[System.Int32]
call static System.String System.String::Format(System.String format, System.Object arg0)
call static System.String System.String::Concat(System.String str0, System.String str1)
call static System.String ClientPlugin.Patches.Miscellaneous.MyGuiScreenMainMenuBasePatch::AppendFrameworkDescription(System.String text)
stloc.2
ldstr "BuildInfo"
ldloc.2
ldloc.1
ldc.r4 0.6
ldsfld VRageMath.Vector4 Sandbox.Graphics.GUI.MyGuiConstants::LABEL_TEXT_COLOR
ldarg.0
ldfld System.Single Sandbox.Graphics.GUI.MyGuiScreenBase::m_transitionAlpha
call static VRageMath.Vector4 VRageMath.Vector4::op_Multiply(VRageMath.Vector4 value1, System.Single scaleFactor)
call static VRageMath.Color VRageMath.Color::op_Implicit(VRageMath.Vector4 v)
ldc.r4 0.6
newobj System.Void VRageMath.Color::.ctor(VRageMath.Color color, System.Single a)
newobj System.Void System.Nullable`1<VRageMath.Color>::.ctor(VRageMath.Color value)
ldc.i4.8
ldc.i4.0
ldc.r4 Infinity
ldc.i4.0
call static System.Void Sandbox.Graphics.MyGuiManager::DrawString(System.String font, System.String text, VRageMath.Vector2 normalizedCoord, System.Single scale, System.Nullable`1<VRageMath.Color> colorMask, VRage.Utils.MyGuiDrawAlignEnum drawAlign, System.Boolean useFullClientArea, System.Single maxTextWidth, System.Boolean ignoreBounds)
ret
