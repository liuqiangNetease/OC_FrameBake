
set(Build_Dir "${Export_Dir}/${Dll_Shared}")
set(Proj_Name "OC")
set(Output_Name "OC")
set(RootNamespace "OC")


if(${UNITY_EDITOR} MATCHES True)
else()
	list(APPEND Ignore_List "/Editor/")
endif(${UNITY_EDITOR} MATCHES True)

include("../cmake_common_lib.txt")


set_property(TARGET ${Proj_Name} PROPERTY VS_DOTNET_REFERENCES
	"System"
	"System.Core"
	"System.Data"
	"System.Data.DataSetExtensions"
	"System.Xml"
	"System.Xml.Linq"
	"${Root_Dir}/../UnityPackages/ThirdParty/Unity/${UNITY_VERSION}/UnityEngine.dll"
	"${Root_Dir}/../UnityPackages/ThirdParty/Unity/${UNITY_VERSION}/UnityEditor.dll")
