#include "pch-cpp.hpp"





template <typename R>
struct VirtualFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_virtual_invoke_data(slot, obj);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};
struct InterfaceActionInvoker0
{
	typedef void (*Action)(void*, const RuntimeMethod*);

	static inline void Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		((Action)invokeData.methodPtr)(obj, invokeData.method);
	}
};
template <typename R>
struct InterfaceFuncInvoker0
{
	typedef R (*Func)(void*, const RuntimeMethod*);

	static inline R Invoke (Il2CppMethodSlot slot, RuntimeClass* declaringInterface, RuntimeObject* obj)
	{
		const VirtualInvokeData& invokeData = il2cpp_codegen_get_interface_invoke_data(slot, obj, declaringInterface);
		return ((Func)invokeData.methodPtr)(obj, invokeData.method);
	}
};

struct Collection_1_tFCFDED5321BE15CA8D30D61CF04DDE693BB0CB5B;
struct IEnumerator_1_tA218C3658C89562941B7435E73E48E2EDC26D9BD;
struct CharU5BU5D_t799905CF001DD5F13F7DBB310181FC4D8B7D0AAB;
struct IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832;
struct NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080;
struct StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF;
struct UInt16U5BU5D_tEB7C42D811D999D2AA815BADC3FCCDD9C67B3F83;
struct Exception_t;
struct IDictionary_t6D03155AF1FA9083817AA5B6AD7DEEACC26AB220;
struct IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484;
struct IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F;
struct NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A;
struct SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6;
struct String_t;
struct UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;

IL2CPP_EXTERN_C RuntimeClass* Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* EarlyInitHelpers_tA67F29CEEF85CD33340F1A46E13686C44F97695A_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Exception_t_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_1_tA218C3658C89562941B7435E73E48E2EDC26D9BD_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C RuntimeClass* Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod* NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714_RuntimeMethod_var;
struct Exception_t_marshaled_com;
struct Exception_t_marshaled_pinvoke;

struct NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CPrivateImplementationDetailsU3E_t9E8035CA1016D615F9AF12233B74AB22D254CFED  : public RuntimeObject
{
};
struct ConnectionMediumUtilities_tCB6702D833B397438D59287BCB7BEA4350099A77  : public RuntimeObject
{
};
struct IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484  : public RuntimeObject
{
	uint32_t ____addressOrScopeId;
	UInt16U5BU5D_tEB7C42D811D999D2AA815BADC3FCCDD9C67B3F83* ____numbers;
	String_t* ____toString;
	int32_t ____hashCode;
};
struct IPAddressInformation_t88AEE176C5711B91C890536A43B17C95690A07A7  : public RuntimeObject
{
};
struct IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F  : public RuntimeObject
{
};
struct NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A  : public RuntimeObject
{
};
struct UnicastIPAddressInformationCollection_tB4D61C9127DFB4168CA3855020CCEB59B75C6EDA  : public RuntimeObject
{
	Collection_1_tFCFDED5321BE15CA8D30D61CF04DDE693BB0CB5B* ___addresses;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct __JobReflectionRegistrationOutput__12582928334401220093_t4BF479A4FEBF98BC8B9CA4D79015AA5FB5D7BFF4  : public RuntimeObject
{
};
struct BandwidthStatistics_tC99802D959F417BC13E66EB26BE5380014B1279F 
{
	float ___Mean;
	float ___Current;
	float ___Minimum;
	float ___Maximum;
	float ___MaximumBurst;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Double_tE150EF3D1D43DEE85D533810AB4C742307EEDE5F 
{
	double ___m_value;
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2  : public ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_pinvoke
{
};
struct Enum_t2A1A94B24E3B776EEF4E5E485E290BB9D4D072E2_marshaled_com
{
};
#pragma pack(push, tp, 1)
struct FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 
{
	union
	{
		struct
		{
			union
			{
				#pragma pack(push, tp, 1)
				struct
				{
					uint64_t ___byte0000;
				};
				#pragma pack(pop, tp)
				struct
				{
					uint64_t ___byte0000_forAlignmentOnly;
				};
				#pragma pack(push, tp, 1)
				struct
				{
					char ___byte0008_OffsetPadding[8];
					uint64_t ___byte0008;
				};
				#pragma pack(pop, tp)
				struct
				{
					char ___byte0008_OffsetPadding_forAlignmentOnly[8];
					uint64_t ___byte0008_forAlignmentOnly;
				};
			};
		};
		uint8_t FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4__padding[16];
	};
};
#pragma pack(pop, tp)
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct Int64_t092CFB123BE63C28ACDAF65C68F21A526050DBA3 
{
	int64_t ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
};
struct RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 
{
	uint32_t ___m_Count;
	uint32_t ___m_Minimum;
	uint32_t ___m_Maximum;
	float ___m_Mean;
	float ___m_MeanDist2;
};
struct Single_t4530F2FF86FCB0DC29F35385CA1BD21BE294761C 
{
	float ___m_value;
};
struct UInt32_t1833D51FFA667B18A5AA4B8D34DE284F8495D29B 
{
	uint32_t ___m_value;
};
struct UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3  : public IPAddressInformation_t88AEE176C5711B91C890536A43B17C95690A07A7
{
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D12406_t627D5C5A5093F3E60AB0DAD78E41F791C9B18AB7 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D12406_t627D5C5A5093F3E60AB0DAD78E41F791C9B18AB7__padding[12406];
	};
};
#pragma pack(pop, tp)
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D256_tFDD17A52FAC33141AF831DBE662D3019639334D8 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D256_tFDD17A52FAC33141AF831DBE662D3019639334D8__padding[256];
	};
};
#pragma pack(pop, tp)
#pragma pack(push, tp, 1)
struct __StaticArrayInitTypeSizeU3D8772_t9FB106A90BF352A44506DD3D6380F6A9429D8125 
{
	union
	{
		struct
		{
			union
			{
			};
		};
		uint8_t __StaticArrayInitTypeSizeU3D8772_t9FB106A90BF352A44506DD3D6380F6A9429D8125__padding[8772];
	};
};
#pragma pack(pop, tp)
struct InternalData_t7825C14D736C88C288CBF752410AFB903635C3D5 
{
	uint64_t ___TotalBytes;
	uint64_t ___PeriodBytes;
	uint64_t ___MinimumPeriodBytes;
	uint64_t ___MaximumPeriodBytes;
	int64_t ___StartTime;
	int64_t ___LastSampleTime;
	float ___MaximumBurst;
};
struct Sample_t3BE5915021631BD79173E66EC224867714256407 
{
	int64_t ___Time;
	uint64_t ___Bytes;
};
struct LatencyStatistics_tB99060D63B5EA9B403706198036545AD40EEEF67 
{
	float ___Mean;
	float ___StandardDeviation;
	uint32_t ___Minimum;
	uint32_t ___Maximum;
	uint32_t ___Current;
	float ___SmoothedCurrent;
};
struct U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46 
{
	union
	{
		struct
		{
			uint32_t ___FixedElementField;
		};
		uint8_t U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46__padding[20];
	};
};
struct Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5 
{
	int32_t ___PacketsReceived;
	int32_t ___PacketsSent;
	int32_t ___AcksReceived;
	int32_t ___AcksSent;
	int32_t ___PacketsDropped;
	int32_t ___PacketsOutOfOrder;
	int32_t ___PacketsDuplicated;
	int32_t ___PacketsStale;
	int32_t ___PacketsResent;
};
struct LongDoubleUnion_tD71C400B6C4CD1A7F13CE8125AC6BBC7A22791CA 
{
	union
	{
		#pragma pack(push, tp, 1)
		struct
		{
			int64_t ___longValue;
		};
		#pragma pack(pop, tp)
		struct
		{
			int64_t ___longValue_forAlignmentOnly;
		};
		#pragma pack(push, tp, 1)
		struct
		{
			double ___doubleValue;
		};
		#pragma pack(pop, tp)
		struct
		{
			double ___doubleValue_forAlignmentOnly;
		};
	};
};
struct ByReference_1_t9C85BCCAAF8C525B6C06B07E922D8D217BE8D6FC 
{
	intptr_t ____value;
};
struct Allocator_t996642592271AAD9EE688F142741D512C07B5824 
{
	int32_t ___value__;
};
struct ConnectionMedium_tB0B1F120F0D0A7DE6A5416DB029530159CEE0EDB 
{
	int32_t ___value__;
};
struct ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E 
{
	LatencyStatistics_tB99060D63B5EA9B403706198036545AD40EEEF67 ___Latency;
	Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5 ___Reliable;
};
struct Exception_t  : public RuntimeObject
{
	String_t* ____className;
	String_t* ____message;
	RuntimeObject* ____data;
	Exception_t* ____innerException;
	String_t* ____helpURL;
	RuntimeObject* ____stackTrace;
	String_t* ____stackTraceString;
	String_t* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	RuntimeObject* ____dynamicMethods;
	int32_t ____HResult;
	String_t* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	IntPtrU5BU5D_tFD177F8C806A6921AD7150264CCC62FA00CAD832* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
struct Exception_t_marshaled_pinvoke
{
	char* ____className;
	char* ____message;
	RuntimeObject* ____data;
	Exception_t_marshaled_pinvoke* ____innerException;
	char* ____helpURL;
	Il2CppIUnknown* ____stackTrace;
	char* ____stackTraceString;
	char* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	Il2CppIUnknown* ____dynamicMethods;
	int32_t ____HResult;
	char* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	Il2CppSafeArray* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
struct Exception_t_marshaled_com
{
	Il2CppChar* ____className;
	Il2CppChar* ____message;
	RuntimeObject* ____data;
	Exception_t_marshaled_com* ____innerException;
	Il2CppChar* ____helpURL;
	Il2CppIUnknown* ____stackTrace;
	Il2CppChar* ____stackTraceString;
	Il2CppChar* ____remoteStackTraceString;
	int32_t ____remoteStackIndex;
	Il2CppIUnknown* ____dynamicMethods;
	int32_t ____HResult;
	Il2CppChar* ____source;
	SafeSerializationManager_tCBB85B95DFD1634237140CD892E82D06ECB3F5E6* ____safeSerializationManager;
	StackTraceU5BU5D_t32FBCB20930EAF5BAE3F450FF75228E5450DA0DF* ___captured_traces;
	Il2CppSafeArray* ___native_trace_ips;
	int32_t ___caught_in_unmanaged;
};
#pragma pack(push, tp, 1)
struct FixedBytes64Align8_t84631A2A3E4A6CEF77C84D9B630BDF9720B945E1 
{
	union
	{
		struct
		{
			union
			{
				#pragma pack(push, tp, 1)
				struct
				{
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0000;
				};
				#pragma pack(pop, tp)
				struct
				{
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0000_forAlignmentOnly;
				};
				#pragma pack(push, tp, 1)
				struct
				{
					char ___offset0016_OffsetPadding[16];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0016;
				};
				#pragma pack(pop, tp)
				struct
				{
					char ___offset0016_OffsetPadding_forAlignmentOnly[16];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0016_forAlignmentOnly;
				};
				#pragma pack(push, tp, 1)
				struct
				{
					char ___offset0032_OffsetPadding[32];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0032;
				};
				#pragma pack(pop, tp)
				struct
				{
					char ___offset0032_OffsetPadding_forAlignmentOnly[32];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0032_forAlignmentOnly;
				};
				#pragma pack(push, tp, 1)
				struct
				{
					char ___offset0048_OffsetPadding[48];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0048;
				};
				#pragma pack(pop, tp)
				struct
				{
					char ___offset0048_OffsetPadding_forAlignmentOnly[48];
					FixedBytes16Align8_t94D49B0852778B92D3912ABC4979B11ADF6ECEE4 ___offset0048_forAlignmentOnly;
				};
			};
		};
		uint8_t FixedBytes64Align8_t84631A2A3E4A6CEF77C84D9B630BDF9720B945E1__padding[64];
	};
};
#pragma pack(pop, tp)
struct NetworkFamily_t76D1D6633F5436F7A1CF96E07D478822EBCD07D6 
{
	int32_t ___value__;
};
struct NetworkInterfaceType_tCE82F24FD42FD5F37905C59DAC97962B0C22FE9C 
{
	int32_t ___value__;
};
struct NetworkReachability_tBAA4C61FCCBE7809DBDD06AE02510392A22E2366 
{
	int32_t ___value__;
};
struct PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E 
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 ___m_RunningStats;
	U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46 ___m_PacketsBySize;
};
struct RuntimePlatform_t9A8AAF204603076FCAAECCCC05DA386AEE7BF66E 
{
	int32_t ___value__;
};
struct FixedList64Bytes_1_t137BDA0D26652E438404CA31731069295DAC8E1C 
{
	alignas(8) FixedBytes64Align8_t84631A2A3E4A6CEF77C84D9B630BDF9720B945E1 ___data;
};
struct NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF 
{
	void* ___m_Buffer;
	int32_t ___m_Length;
	int32_t ___m_AllocatorLabel;
};
struct ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D 
{
	ByReference_1_t9C85BCCAAF8C525B6C06B07E922D8D217BE8D6FC ____pointer;
	int32_t ____length;
};
struct DriverStatistics_tE192BC17390D6FAD30E34565CA381F8FC083DE06 
{
	uint64_t ___RxTotalBytes;
	uint64_t ___TxTotalBytes;
	uint64_t ___RxTotalPackets;
	uint64_t ___TxTotalPackets;
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E ___RxPacketSizes;
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E ___TxPacketSizes;
	BandwidthStatistics_tC99802D959F417BC13E66EB26BE5380014B1279F ___RxBandwidth;
	BandwidthStatistics_tC99802D959F417BC13E66EB26BE5380014B1279F ___TxBandwidth;
	float ___RxMeanQueueUsage;
	float ___TxMeanQueueUsage;
	uint32_t ___RxMaximumQueueUsage;
	uint32_t ___TxMaximumQueueUsage;
};
struct TransferrableData_tE4092BB2D19869881CCCD6B9B3E54FDC904B3197 
{
	FixedList64Bytes_1_t137BDA0D26652E438404CA31731069295DAC8E1C ___m_RawAddressContainer;
};
struct NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC 
{
	TransferrableData_tE4092BB2D19869881CCCD6B9B3E54FDC904B3197 ___Transferrable;
};
struct U3CPrivateImplementationDetailsU3E_t9E8035CA1016D615F9AF12233B74AB22D254CFED_StaticFields
{
	__StaticArrayInitTypeSizeU3D12406_t627D5C5A5093F3E60AB0DAD78E41F791C9B18AB7 ___45C3F53668660851D1A587C730685DF5F9C9E3660041042D34380D666A396996;
	__StaticArrayInitTypeSizeU3D256_tFDD17A52FAC33141AF831DBE662D3019639334D8 ___74EF7306E7452D6859B6463CE496B8DF30925F69E1B2969E1F3F34BBC9C6AF04;
	__StaticArrayInitTypeSizeU3D8772_t9FB106A90BF352A44506DD3D6380F6A9429D8125 ___EF95E52E58E36852E475FAED26682EA2FA3162A0788ECA26BFC48C0A901A2F4C;
};
struct IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484_StaticFields
{
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___Any;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___Loopback;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___Broadcast;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___None;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___IPv6Any;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___IPv6Loopback;
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* ___IPv6None;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct Exception_t_StaticFields
{
	RuntimeObject* ___s_EDILock;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif
struct NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080  : public RuntimeArray
{
	ALIGN_FIELD (8) NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* m_Items[1];

	inline NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A** GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
	inline NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A** GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* value)
	{
		m_Items[index] = value;
		Il2CppCodeGenWriteBarrier((void**)m_Items + index, (void*)value);
	}
};


IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714_gshared (NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58_gshared (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC_gshared (const RuntimeMethod* method) ;

IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool NetworkEndpoint_get_IsLoopback_m995A1746AE5434B65C74F584D23EF2775F3D5BB0 (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Application_get_internetReachability_m3FECA8BA005340369BB952CE8CDF3E1A53F3BA0E (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t ConnectionMediumUtilities_GetMediumFromNetworkInterface_mC33DE8B58ED73D3CADE24B9545702CB6F33DA895 (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC ___0_endpoint, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t Application_get_platform_m59EF7D6155D18891B24767F83F388160B1FF2138 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t NetworkEndpoint_get_Family_mEA511ED9ABB1C7067B4726E134CCC8A30114A6BC (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF NetworkEndpoint_GetRawAddressBytes_m4074E16933836829968AF93F65CB13ED0D347651 (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC* __this, const RuntimeMethod* method) ;
inline ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714 (NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF* __this, const RuntimeMethod* method)
{
	return ((  ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D (*) (NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF*, const RuntimeMethod*))NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714_gshared)(__this, method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void IPAddress__ctor_mD9A747F8398455BFE7D6E488FF020EAA0EBFF45A (IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* __this, ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D ___0_address, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080* NetworkInterface_GetAllNetworkInterfaces_m4E5A4AAEED8B11868BDC8F78975460D9B6F3CD70 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketLossPercent_m6E11BF05A32ACA5D1DCF7AF98691687F9993DD03 (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketDuplicationPercent_mDCA7B5BBC9CDCB151F9C06294543220D78B4764E (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketOutOfOrderPercent_m46DEEB26B1CB8AB0E4B34FFDC8B99EFA505ED148 (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PacketSizeStatistics_get_Mean_m29D2D7C0B72D294E0D6CA6B86131C89F82E43C5D (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float RunningStatistics_get_StandardDeviation_m709BE8A3169775D460002C9F6BBF260BCFE462B9 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PacketSizeStatistics_get_StandardDeviation_m7A98CAF79BD82DC0D10F51A9EC44EF8AC8429E00 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Minimum_m3B14329F2EE92B69E21490FB55D91377A14447A6 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Maximum_m507ED66B447FB9EC4FC13483791C77D8C15ECB42 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_SmallerThan128Bytes_mF1E6601AAF03924E2F05CFC14D53B74D827AD076 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between128And255Bytes_m305F917484BE5D6C96B3055894D2456B6BD80416 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between256And511Bytes_m79D8ECAFA90B3D2E1F858807875C3A1705BA716D (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between512And1023Bytes_mAE6790EA9DC462DB1AE9BB21642E1787D1BCA25E (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_LargerThan1023Bytes_mED82C5C1B0F39B146D6C34413404EEAE4C026FCD (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunningStatistics_AddDataPoint_m526C882C18A65D3923525ACC63C8A97896F297B4 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, uint32_t ___0_value, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t math_lzcnt_m121BDDDEE89F5A401E2E5F0AD900D22E47C8741C_inline (uint32_t ___0_x, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t math_min_m02D43DF516544C279AF660EA4731449C82991849_inline (int32_t ___0_x, int32_t ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PacketSizeStatistics_AddPacket_m7FA17EBA63216E636A40FE2037639A3B0172BECB (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, uint32_t ___0_size, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float math_sqrt_mEF31DE7BD0179009683C5D7B0C58E6571B30CF4A_inline (float ___0_x, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t math_min_mFBB411A5384A9CFD7787E398A6F758553D3700A9_inline (uint32_t ___0_x, uint32_t ___1_y, const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t math_max_mD9D4307218A8CFA92F9C26871E508B23C17F6395_inline (uint32_t ___0_x, uint32_t ___1_y, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunningStatistics_Merge_m48B2C1B6723015C08D6152641882A7BC7923C677 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 ___0_other, const RuntimeMethod* method) ;
inline void IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58 (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58_gshared)(method);
}
inline void IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC (const RuntimeMethod* method)
{
	((  void (*) (const RuntimeMethod*))IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC_gshared)(method);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void EarlyInitHelpers_JobReflectionDataCreationFailed_mD6AB08D5BB411CCE38A87793C3C7062EC91FD1EC (Exception_t* ___0_ex, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void __JobReflectionRegistrationOutput__12582928334401220093_CreateJobReflectionData_mC48194201F255F2ADA41166D6839ADF412E2B950 (const RuntimeMethod* method) ;
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 69364
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t ConnectionMediumUtilities_InferFromLocalEndpointAndCurrentPlatform_m6E3021257B1DECF93363A5778156A3469C3BA9C6 (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC ___0_endpoint, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	int32_t V_0 = 0;
	bool V_1 = false;
	int32_t V_2 = 0;
	bool V_3 = false;
	bool V_4 = false;
	int32_t V_5 = 0;
	int32_t V_6 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:40>
		bool L_0;
		L_0 = NetworkEndpoint_get_IsLoopback_m995A1746AE5434B65C74F584D23EF2775F3D5BB0((&___0_endpoint), NULL);
		V_1 = L_0;
		bool L_1 = V_1;
		if (!L_1)
		{
			goto IL_0010;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:41>
		V_2 = 2;
		goto IL_0083;
	}

IL_0010:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:50>
		il2cpp_codegen_runtime_class_init_inline(Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
		int32_t L_2;
		L_2 = Application_get_internetReachability_m3FECA8BA005340369BB952CE8CDF3E1A53F3BA0E(NULL);
		V_3 = (bool)((((int32_t)L_2) == ((int32_t)1))? 1 : 0);
		bool L_3 = V_3;
		if (!L_3)
		{
			goto IL_0021;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:51>
		V_2 = ((int32_t)16);
		goto IL_0083;
	}

IL_0021:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:54>
		NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC L_4 = ___0_endpoint;
		int32_t L_5;
		L_5 = ConnectionMediumUtilities_GetMediumFromNetworkInterface_mC33DE8B58ED73D3CADE24B9545702CB6F33DA895(L_4, NULL);
		V_0 = L_5;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:55>
		int32_t L_6 = V_0;
		V_4 = (bool)((((int32_t)((((int32_t)L_6) == ((int32_t)1))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		bool L_7 = V_4;
		if (!L_7)
		{
			goto IL_0039;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:56>
		int32_t L_8 = V_0;
		V_2 = L_8;
		goto IL_0083;
	}

IL_0039:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:59>
		il2cpp_codegen_runtime_class_init_inline(Application_tDB03BE91CDF0ACA614A5E0B67CFB77C44EB19B21_il2cpp_TypeInfo_var);
		int32_t L_9;
		L_9 = Application_get_platform_m59EF7D6155D18891B24767F83F388160B1FF2138(NULL);
		V_6 = L_9;
		int32_t L_10 = V_6;
		V_5 = L_10;
		int32_t L_11 = V_5;
		if ((((int32_t)L_11) > ((int32_t)((int32_t)11))))
		{
			goto IL_0059;
		}
	}
	{
		int32_t L_12 = V_5;
		if ((((int32_t)L_12) == ((int32_t)8)))
		{
			goto IL_0073;
		}
	}
	{
		goto IL_0051;
	}

IL_0051:
	{
		int32_t L_13 = V_5;
		if ((((int32_t)L_13) == ((int32_t)((int32_t)11))))
		{
			goto IL_0073;
		}
	}
	{
		goto IL_007f;
	}

IL_0059:
	{
		int32_t L_14 = V_5;
		if ((((int32_t)L_14) == ((int32_t)((int32_t)32))))
		{
			goto IL_007b;
		}
	}
	{
		goto IL_0061;
	}

IL_0061:
	{
		int32_t L_15 = V_5;
		if ((!(((uint32_t)((int32_t)il2cpp_codegen_subtract((int32_t)L_15, ((int32_t)43)))) > ((uint32_t)2))))
		{
			goto IL_0077;
		}
	}
	{
		goto IL_006b;
	}

IL_006b:
	{
		int32_t L_16 = V_5;
		if ((((int32_t)L_16) == ((int32_t)((int32_t)50))))
		{
			goto IL_007b;
		}
	}
	{
		goto IL_007f;
	}

IL_0073:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:65>
		V_2 = 8;
		goto IL_0083;
	}

IL_0077:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:69>
		V_2 = 4;
		goto IL_0083;
	}

IL_007b:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:72>
		V_2 = 8;
		goto IL_0083;
	}

IL_007f:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:74>
		V_2 = 1;
		goto IL_0083;
	}

IL_0083:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:77>
		int32_t L_17 = V_2;
		return L_17;
	}
}
// Method Definition Index: 69365
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t ConnectionMediumUtilities_GetMediumFromNetworkInterface_mC33DE8B58ED73D3CADE24B9545702CB6F33DA895 (NetworkEndpoint_t0F60A2EF4E82ED5F26845E5537772D24C32426AC ___0_endpoint, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_1_tA218C3658C89562941B7435E73E48E2EDC26D9BD_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D V_0;
	memset((&V_0), 0, sizeof(V_0));
	IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* V_1 = NULL;
	bool V_2 = false;
	int32_t V_3 = 0;
	NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF V_4;
	memset((&V_4), 0, sizeof(V_4));
	NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080* V_5 = NULL;
	int32_t V_6 = 0;
	NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* V_7 = NULL;
	IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F* V_8 = NULL;
	RuntimeObject* V_9 = NULL;
	UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3* V_10 = NULL;
	bool V_11 = false;
	int32_t V_12 = 0;
	int32_t V_13 = 0;
	il2cpp::utils::ExceptionSupportStack<RuntimeObject*, 1> __active_exceptions;
	int32_t G_B3_0 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:81>
		int32_t L_0;
		L_0 = NetworkEndpoint_get_Family_mEA511ED9ABB1C7067B4726E134CCC8A30114A6BC((&___0_endpoint), NULL);
		if ((((int32_t)L_0) == ((int32_t)2)))
		{
			goto IL_001b;
		}
	}
	{
		int32_t L_1;
		L_1 = NetworkEndpoint_get_Family_mEA511ED9ABB1C7067B4726E134CCC8A30114A6BC((&___0_endpoint), NULL);
		G_B3_0 = ((((int32_t)((((int32_t)L_1) == ((int32_t)((int32_t)23)))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		goto IL_001c;
	}

IL_001b:
	{
		G_B3_0 = 0;
	}

IL_001c:
	{
		V_2 = (bool)G_B3_0;
		bool L_2 = V_2;
		if (!L_2)
		{
			goto IL_0027;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:82>
		V_3 = 1;
		goto IL_0129;
	}

IL_0027:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:84>
		NativeArray_1_t81F55263465517B73C455D3400CF67B4BADD85CF L_3;
		L_3 = NetworkEndpoint_GetRawAddressBytes_m4074E16933836829968AF93F65CB13ED0D347651((&___0_endpoint), NULL);
		V_4 = L_3;
		ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D L_4;
		L_4 = NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714((&V_4), NativeArray_1_AsReadOnlySpan_m7217090A73A255C701355AC9C7B3513F24E4E714_RuntimeMethod_var);
		V_0 = L_4;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:85>
		ReadOnlySpan_1_tA850A6C0E88ABBA37646A078ACBC24D6D5FD9B4D L_5 = V_0;
		IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* L_6 = (IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484*)il2cpp_codegen_object_new(IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484_il2cpp_TypeInfo_var);
		IPAddress__ctor_mD9A747F8398455BFE7D6E488FF020EAA0EBFF45A(L_6, L_5, NULL);
		V_1 = L_6;
	}
	try
	{
		{
			//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:89>
			NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080* L_7;
			L_7 = NetworkInterface_GetAllNetworkInterfaces_m4E5A4AAEED8B11868BDC8F78975460D9B6F3CD70(NULL);
			V_5 = L_7;
			V_6 = 0;
			goto IL_0112_1;
		}

IL_0050_1:
		{
			//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:89>
			NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080* L_8 = V_5;
			int32_t L_9 = V_6;
			NullCheck(L_8);
			int32_t L_10 = L_9;
			NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* L_11 = (L_8)->GetAt(static_cast<il2cpp_array_size_t>(L_10));
			V_7 = L_11;
			//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:91>
			NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* L_12 = V_7;
			NullCheck(L_12);
			IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F* L_13;
			L_13 = VirtualFuncInvoker0< IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F* >::Invoke(4, L_12);
			V_8 = L_13;
			//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:92>
			IPInterfaceProperties_t60A00D504E4F72CAFE4C0AE4DA6A062B44D1512F* L_14 = V_8;
			NullCheck(L_14);
			UnicastIPAddressInformationCollection_tB4D61C9127DFB4168CA3855020CCEB59B75C6EDA* L_15;
			L_15 = VirtualFuncInvoker0< UnicastIPAddressInformationCollection_tB4D61C9127DFB4168CA3855020CCEB59B75C6EDA* >::Invoke(4, L_14);
			NullCheck(L_15);
			RuntimeObject* L_16;
			L_16 = VirtualFuncInvoker0< RuntimeObject* >::Invoke(18, L_15);
			V_9 = L_16;
		}
		{
			auto __finallyBlock = il2cpp::utils::Finally([&]
			{

FINALLY_00fe_1:
				{
					{
						RuntimeObject* L_17 = V_9;
						if (!L_17)
						{
							goto IL_010a_1;
						}
					}
					{
						RuntimeObject* L_18 = V_9;
						NullCheck(L_18);
						InterfaceActionInvoker0::Invoke(0, IDisposable_t030E0496B4E0E4E4F086825007979AF51F7248C5_il2cpp_TypeInfo_var, L_18);
					}

IL_010a_1:
					{
						return;
					}
				}
			});
			try
			{
				{
					goto IL_00f0_2;
				}

IL_0072_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:92>
					RuntimeObject* L_19 = V_9;
					NullCheck(L_19);
					UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3* L_20;
					L_20 = InterfaceFuncInvoker0< UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3* >::Invoke(0, IEnumerator_1_tA218C3658C89562941B7435E73E48E2EDC26D9BD_il2cpp_TypeInfo_var, L_19);
					V_10 = L_20;
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:94>
					UnicastIPAddressInformation_t4ACCADE9FBC1F8243A602439C94301E2C30295F3* L_21 = V_10;
					NullCheck(L_21);
					IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* L_22;
					L_22 = VirtualFuncInvoker0< IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* >::Invoke(4, L_21);
					IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484* L_23 = V_1;
					V_11 = (bool)((((RuntimeObject*)(IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484*)L_22) == ((RuntimeObject*)(IPAddress_t2F4486449B0D73FF2D3B36A9FE5E9C3F63116484*)L_23))? 1 : 0);
					bool L_24 = V_11;
					if (!L_24)
					{
						goto IL_00ef_2;
					}
				}
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:99>
					NetworkInterface_t3E7DDFADB8912D0F9D001D1195EBD0FB08B7F47A* L_25 = V_7;
					NullCheck(L_25);
					int32_t L_26;
					L_26 = VirtualFuncInvoker0< int32_t >::Invoke(5, L_25);
					V_13 = L_26;
					int32_t L_27 = V_13;
					V_12 = L_27;
					int32_t L_28 = V_12;
					if ((((int32_t)L_28) > ((int32_t)((int32_t)71))))
					{
						goto IL_00b7_2;
					}
				}
				{
					int32_t L_29 = V_12;
					if ((((int32_t)L_29) == ((int32_t)1)))
					{
						goto IL_00da_2;
					}
				}
				{
					goto IL_00a7_2;
				}

IL_00a7_2:
				{
					int32_t L_30 = V_12;
					if ((((int32_t)L_30) == ((int32_t)((int32_t)24))))
					{
						goto IL_00de_2;
					}
				}
				{
					goto IL_00af_2;
				}

IL_00af_2:
				{
					int32_t L_31 = V_12;
					if ((((int32_t)L_31) == ((int32_t)((int32_t)71))))
					{
						goto IL_00e2_2;
					}
				}
				{
					goto IL_00eb_2;
				}

IL_00b7_2:
				{
					int32_t L_32 = V_12;
					if ((((int32_t)L_32) == ((int32_t)((int32_t)131))))
					{
						goto IL_00da_2;
					}
				}
				{
					goto IL_00c2_2;
				}

IL_00c2_2:
				{
					int32_t L_33 = V_12;
					if ((((int32_t)L_33) == ((int32_t)((int32_t)237))))
					{
						goto IL_00e6_2;
					}
				}
				{
					goto IL_00cd_2;
				}

IL_00cd_2:
				{
					int32_t L_34 = V_12;
					if ((!(((uint32_t)((int32_t)il2cpp_codegen_subtract((int32_t)L_34, ((int32_t)243)))) > ((uint32_t)1))))
					{
						goto IL_00e6_2;
					}
				}
				{
					goto IL_00eb_2;
				}

IL_00da_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:103>
					V_3 = 1;
					goto IL_0129;
				}

IL_00de_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:105>
					V_3 = 2;
					goto IL_0129;
				}

IL_00e2_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:107>
					V_3 = 8;
					goto IL_0129;
				}

IL_00e6_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:111>
					V_3 = ((int32_t)16);
					goto IL_0129;
				}

IL_00eb_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:113>
					V_3 = 4;
					goto IL_0129;
				}

IL_00ef_2:
				{
				}

IL_00f0_2:
				{
					//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:92>
					RuntimeObject* L_35 = V_9;
					NullCheck(L_35);
					bool L_36;
					L_36 = InterfaceFuncInvoker0< bool >::Invoke(0, IEnumerator_t7B609C2FFA6EB5167D9C62A0C32A21DE2F666DAA_il2cpp_TypeInfo_var, L_35);
					if (L_36)
					{
						goto IL_0072_2;
					}
				}
				{
					goto IL_010b_1;
				}
			}
			catch(Il2CppExceptionWrapper& e)
			{
				__finallyBlock.StoreException(e.ex);
			}
		}

IL_010b_1:
		{
			int32_t L_37 = V_6;
			V_6 = ((int32_t)il2cpp_codegen_add(L_37, 1));
		}

IL_0112_1:
		{
			//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:89>
			int32_t L_38 = V_6;
			NetworkInterfaceU5BU5D_t62783E27F1C4A989B118CDBBE2FCBE65EE5CA080* L_39 = V_5;
			NullCheck(L_39);
			if ((((int32_t)L_38) < ((int32_t)((int32_t)(((RuntimeArray*)L_39)->max_length)))))
			{
				goto IL_0050_1;
			}
		}
		{
			goto IL_0125;
		}
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (il2cpp_defaults.object_class, il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_0120;
		}
		throw e;
	}

CATCH_0120:
	{
		RuntimeObject* L_40 = ((RuntimeObject*)IL2CPP_GET_ACTIVE_EXCEPTION(RuntimeObject*));;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:119>
		IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
		goto IL_0125;
	}

IL_0125:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:124>
		V_3 = 1;
		goto IL_0129;
	}

IL_0129:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionMedium.cs:125>
		int32_t L_41 = V_3;
		return L_41;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 69366
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketLossPercent_m6E11BF05A32ACA5D1DCF7AF98691687F9993DD03 (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t V_1 = 0;
	float V_2 = 0.0f;
	float G_B3_0 = 0.0f;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:66>
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_0 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_1 = L_0->___PacketsReceived;
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_2 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_3 = L_2->___PacketsSent;
		V_0 = ((int32_t)il2cpp_codegen_add(L_1, L_3));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:67>
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_4 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_5 = L_4->___PacketsDropped;
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_6 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_7 = L_6->___PacketsResent;
		V_1 = ((int32_t)il2cpp_codegen_add(L_5, L_7));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:68>
		int32_t L_8 = V_0;
		if ((((int32_t)L_8) > ((int32_t)0)))
		{
			goto IL_003c;
		}
	}
	{
		G_B3_0 = (0.0f);
		goto IL_0047;
	}

IL_003c:
	{
		int32_t L_9 = V_1;
		int32_t L_10 = V_0;
		G_B3_0 = ((float)(((float)il2cpp_codegen_multiply(((float)L_9), (100.0f)))/((float)L_10)));
	}

IL_0047:
	{
		V_2 = G_B3_0;
		goto IL_004a;
	}

IL_004a:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:69>
		float L_11 = V_2;
		return L_11;
	}
}
IL2CPP_EXTERN_C  float ConnectionStatistics_get_PacketLossPercent_m6E11BF05A32ACA5D1DCF7AF98691687F9993DD03_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E*>(__this + _offset);
	float _returnValue;
	_returnValue = ConnectionStatistics_get_PacketLossPercent_m6E11BF05A32ACA5D1DCF7AF98691687F9993DD03(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69367
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketDuplicationPercent_mDCA7B5BBC9CDCB151F9C06294543220D78B4764E (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) 
{
	float G_B3_0 = 0.0f;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:73>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:74>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:75>
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_0 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_1 = L_0->___PacketsReceived;
		if ((((int32_t)L_1) > ((int32_t)0)))
		{
			goto IL_0015;
		}
	}
	{
		G_B3_0 = (0.0f);
		goto IL_0034;
	}

IL_0015:
	{
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_2 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_3 = L_2->___PacketsDuplicated;
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_4 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_5 = L_4->___PacketsReceived;
		G_B3_0 = ((float)(((float)il2cpp_codegen_multiply(((float)L_3), (100.0f)))/((float)L_5)));
	}

IL_0034:
	{
		return G_B3_0;
	}
}
IL2CPP_EXTERN_C  float ConnectionStatistics_get_PacketDuplicationPercent_mDCA7B5BBC9CDCB151F9C06294543220D78B4764E_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E*>(__this + _offset);
	float _returnValue;
	_returnValue = ConnectionStatistics_get_PacketDuplicationPercent_mDCA7B5BBC9CDCB151F9C06294543220D78B4764E(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69368
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float ConnectionStatistics_get_PacketOutOfOrderPercent_m46DEEB26B1CB8AB0E4B34FFDC8B99EFA505ED148 (ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* __this, const RuntimeMethod* method) 
{
	float G_B3_0 = 0.0f;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:78>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:79>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/ConnectionStatistics.cs:80>
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_0 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_1 = L_0->___PacketsReceived;
		if ((((int32_t)L_1) > ((int32_t)0)))
		{
			goto IL_0015;
		}
	}
	{
		G_B3_0 = (0.0f);
		goto IL_0034;
	}

IL_0015:
	{
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_2 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_3 = L_2->___PacketsOutOfOrder;
		Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5* L_4 = (Statistics_t11CF93D4C83ABFDDFD0BBAD848B752181939A2E5*)(&__this->___Reliable);
		int32_t L_5 = L_4->___PacketsReceived;
		G_B3_0 = ((float)(((float)il2cpp_codegen_multiply(((float)L_3), (100.0f)))/((float)L_5)));
	}

IL_0034:
	{
		return G_B3_0;
	}
}
IL2CPP_EXTERN_C  float ConnectionStatistics_get_PacketOutOfOrderPercent_m46DEEB26B1CB8AB0E4B34FFDC8B99EFA505ED148_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<ConnectionStatistics_tE2C55516B2ABB001F04A3A34D0AC37A22A00295E*>(__this + _offset);
	float _returnValue;
	_returnValue = ConnectionStatistics_get_PacketOutOfOrderPercent_m46DEEB26B1CB8AB0E4B34FFDC8B99EFA505ED148(_thisAdjusted, method);
	return _returnValue;
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 69369
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PacketSizeStatistics_get_Mean_m29D2D7C0B72D294E0D6CA6B86131C89F82E43C5D (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:41>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* L_0 = (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)(&__this->___m_RunningStats);
		float L_1;
		L_1 = RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843_inline(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C  float PacketSizeStatistics_get_Mean_m29D2D7C0B72D294E0D6CA6B86131C89F82E43C5D_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	float _returnValue;
	_returnValue = PacketSizeStatistics_get_Mean_m29D2D7C0B72D294E0D6CA6B86131C89F82E43C5D(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69370
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float PacketSizeStatistics_get_StandardDeviation_m7A98CAF79BD82DC0D10F51A9EC44EF8AC8429E00 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:44>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* L_0 = (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)(&__this->___m_RunningStats);
		float L_1;
		L_1 = RunningStatistics_get_StandardDeviation_m709BE8A3169775D460002C9F6BBF260BCFE462B9(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C  float PacketSizeStatistics_get_StandardDeviation_m7A98CAF79BD82DC0D10F51A9EC44EF8AC8429E00_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	float _returnValue;
	_returnValue = PacketSizeStatistics_get_StandardDeviation_m7A98CAF79BD82DC0D10F51A9EC44EF8AC8429E00(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69371
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Minimum_m3B14329F2EE92B69E21490FB55D91377A14447A6 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:47>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* L_0 = (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)(&__this->___m_RunningStats);
		uint32_t L_1;
		L_1 = RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD_inline(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_Minimum_m3B14329F2EE92B69E21490FB55D91377A14447A6_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_Minimum_m3B14329F2EE92B69E21490FB55D91377A14447A6(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69372
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Maximum_m507ED66B447FB9EC4FC13483791C77D8C15ECB42 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:50>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* L_0 = (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)(&__this->___m_RunningStats);
		uint32_t L_1;
		L_1 = RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53_inline(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_Maximum_m507ED66B447FB9EC4FC13483791C77D8C15ECB42_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_Maximum_m507ED66B447FB9EC4FC13483791C77D8C15ECB42(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69373
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_SmallerThan128Bytes_mF1E6601AAF03924E2F05CFC14D53B74D827AD076 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:53>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_0 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_1 = (uint32_t*)(&L_0->___FixedElementField);
		int32_t L_2 = *((uint32_t*)L_1);
		return L_2;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_SmallerThan128Bytes_mF1E6601AAF03924E2F05CFC14D53B74D827AD076_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_SmallerThan128Bytes_mF1E6601AAF03924E2F05CFC14D53B74D827AD076(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69374
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between128And255Bytes_m305F917484BE5D6C96B3055894D2456B6BD80416 (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:56>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_0 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_1 = (uint32_t*)(&L_0->___FixedElementField);
		int32_t L_2 = *((uint32_t*)((uint32_t*)il2cpp_codegen_add((intptr_t)L_1, 4)));
		return L_2;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_Between128And255Bytes_m305F917484BE5D6C96B3055894D2456B6BD80416_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_Between128And255Bytes_m305F917484BE5D6C96B3055894D2456B6BD80416(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69375
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between256And511Bytes_m79D8ECAFA90B3D2E1F858807875C3A1705BA716D (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:59>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_0 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_1 = (uint32_t*)(&L_0->___FixedElementField);
		int32_t L_2 = *((uint32_t*)((uint32_t*)il2cpp_codegen_add((intptr_t)L_1, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)2), 4)))));
		return L_2;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_Between256And511Bytes_m79D8ECAFA90B3D2E1F858807875C3A1705BA716D_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_Between256And511Bytes_m79D8ECAFA90B3D2E1F858807875C3A1705BA716D(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69376
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_Between512And1023Bytes_mAE6790EA9DC462DB1AE9BB21642E1787D1BCA25E (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:62>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_0 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_1 = (uint32_t*)(&L_0->___FixedElementField);
		int32_t L_2 = *((uint32_t*)((uint32_t*)il2cpp_codegen_add((intptr_t)L_1, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)3), 4)))));
		return L_2;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_Between512And1023Bytes_mAE6790EA9DC462DB1AE9BB21642E1787D1BCA25E_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_Between512And1023Bytes_mAE6790EA9DC462DB1AE9BB21642E1787D1BCA25E(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69377
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t PacketSizeStatistics_get_LargerThan1023Bytes_mED82C5C1B0F39B146D6C34413404EEAE4C026FCD (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:65>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_0 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_1 = (uint32_t*)(&L_0->___FixedElementField);
		int32_t L_2 = *((uint32_t*)((uint32_t*)il2cpp_codegen_add((intptr_t)L_1, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)4), 4)))));
		return L_2;
	}
}
IL2CPP_EXTERN_C  uint32_t PacketSizeStatistics_get_LargerThan1023Bytes_mED82C5C1B0F39B146D6C34413404EEAE4C026FCD_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = PacketSizeStatistics_get_LargerThan1023Bytes_mED82C5C1B0F39B146D6C34413404EEAE4C026FCD(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69378
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void PacketSizeStatistics_AddPacket_m7FA17EBA63216E636A40FE2037639A3B0172BECB (PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* __this, uint32_t ___0_size, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:69>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* L_0 = (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)(&__this->___m_RunningStats);
		uint32_t L_1 = ___0_size;
		RunningStatistics_AddDataPoint_m526C882C18A65D3923525ACC63C8A97896F297B4(L_0, L_1, NULL);
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:70>
		U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46* L_2 = (U3Cm_PacketsBySizeU3Ee__FixedBuffer_t25C20255526B7267E7D8A33C3E49D87836B66D46*)(&__this->___m_PacketsBySize);
		uint32_t* L_3 = (uint32_t*)(&L_2->___FixedElementField);
		uint32_t L_4 = ___0_size;
		int32_t L_5;
		L_5 = math_lzcnt_m121BDDDEE89F5A401E2E5F0AD900D22E47C8741C_inline(((int32_t)((uint32_t)L_4>>7)), NULL);
		int32_t L_6;
		L_6 = math_min_m02D43DF516544C279AF660EA4731449C82991849_inline(4, ((int32_t)il2cpp_codegen_subtract(((int32_t)32), L_5)), NULL);
		uint32_t* L_7 = ((uint32_t*)il2cpp_codegen_add((intptr_t)L_3, ((intptr_t)il2cpp_codegen_multiply(((intptr_t)L_6), 4))));
		int32_t L_8 = *((uint32_t*)L_7);
		*((int32_t*)L_7) = (int32_t)((int32_t)il2cpp_codegen_add(L_8, 1));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/PacketSizeStatistics.cs:71>
		return;
	}
}
IL2CPP_EXTERN_C  void PacketSizeStatistics_AddPacket_m7FA17EBA63216E636A40FE2037639A3B0172BECB_AdjustorThunk (RuntimeObject* __this, uint32_t ___0_size, const RuntimeMethod* method)
{
	PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<PacketSizeStatistics_tBDDACAF067E8F485D401C527C7C90C6B7220041E*>(__this + _offset);
	PacketSizeStatistics_AddPacket_m7FA17EBA63216E636A40FE2037639A3B0172BECB(_thisAdjusted, ___0_size, method);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 69379
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:25>
		uint32_t L_0 = __this->___m_Minimum;
		return L_0;
	}
}
IL2CPP_EXTERN_C  uint32_t RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD_inline(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69380
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:28>
		uint32_t L_0 = __this->___m_Maximum;
		return L_0;
	}
}
IL2CPP_EXTERN_C  uint32_t RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	uint32_t _returnValue;
	_returnValue = RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53_inline(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69381
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:31>
		float L_0 = __this->___m_Mean;
		return L_0;
	}
}
IL2CPP_EXTERN_C  float RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	float _returnValue;
	_returnValue = RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843_inline(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69382
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float RunningStatistics_get_StandardDeviation_m709BE8A3169775D460002C9F6BBF260BCFE462B9 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:34>
		float L_0;
		L_0 = RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581(__this, NULL);
		float L_1;
		L_1 = math_sqrt_mEF31DE7BD0179009683C5D7B0C58E6571B30CF4A_inline(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C  float RunningStatistics_get_StandardDeviation_m709BE8A3169775D460002C9F6BBF260BCFE462B9_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	float _returnValue;
	_returnValue = RunningStatistics_get_StandardDeviation_m709BE8A3169775D460002C9F6BBF260BCFE462B9(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69383
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	float G_B3_0 = 0.0f;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:37>
		uint32_t L_0 = __this->___m_Count;
		if ((!(((uint32_t)L_0) > ((uint32_t)1))))
		{
			goto IL_001c;
		}
	}
	{
		float L_1 = __this->___m_MeanDist2;
		uint32_t L_2 = __this->___m_Count;
		G_B3_0 = ((float)(L_1/((float)((double)(uint32_t)((int32_t)il2cpp_codegen_subtract((int32_t)L_2, 1))))));
		goto IL_0021;
	}

IL_001c:
	{
		G_B3_0 = (0.0f);
	}

IL_0021:
	{
		return G_B3_0;
	}
}
IL2CPP_EXTERN_C  float RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581_AdjustorThunk (RuntimeObject* __this, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	float _returnValue;
	_returnValue = RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581(_thisAdjusted, method);
	return _returnValue;
}
// Method Definition Index: 69384
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunningStatistics_AddDataPoint_m526C882C18A65D3923525ACC63C8A97896F297B4 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, uint32_t ___0_value, const RuntimeMethod* method) 
{
	bool V_0 = false;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	bool V_4 = false;
	bool V_5 = false;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:43>
		uint32_t L_0 = __this->___m_Count;
		__this->___m_Count = ((int32_t)il2cpp_codegen_add((int32_t)L_0, 1));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:47>
		uint32_t L_1 = __this->___m_Count;
		V_0 = (bool)((((int32_t)((!(((uint32_t)L_1) <= ((uint32_t)1)))? 1 : 0)) == ((int32_t)0))? 1 : 0);
		bool L_2 = V_0;
		if (!L_2)
		{
			goto IL_004c;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:49>
		__this->___m_Count = 1;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:50>
		uint32_t L_3 = ___0_value;
		__this->___m_Minimum = L_3;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:51>
		uint32_t L_4 = ___0_value;
		__this->___m_Maximum = L_4;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:52>
		uint32_t L_5 = ___0_value;
		__this->___m_Mean = ((float)((double)(uint32_t)L_5));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:53>
		__this->___m_MeanDist2 = (0.0f);
		goto IL_00b7;
	}

IL_004c:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:57>
		uint32_t L_6 = ___0_value;
		uint32_t L_7 = __this->___m_Minimum;
		V_4 = (bool)((!(((uint32_t)L_6) >= ((uint32_t)L_7)))? 1 : 0);
		bool L_8 = V_4;
		if (!L_8)
		{
			goto IL_0063;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:58>
		uint32_t L_9 = ___0_value;
		__this->___m_Minimum = L_9;
	}

IL_0063:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:59>
		uint32_t L_10 = ___0_value;
		uint32_t L_11 = __this->___m_Maximum;
		V_5 = (bool)((!(((uint32_t)L_10) <= ((uint32_t)L_11)))? 1 : 0);
		bool L_12 = V_5;
		if (!L_12)
		{
			goto IL_0079;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:60>
		uint32_t L_13 = ___0_value;
		__this->___m_Maximum = L_13;
	}

IL_0079:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:62>
		uint32_t L_14 = ___0_value;
		V_1 = ((float)((double)(uint32_t)L_14));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:63>
		float L_15 = V_1;
		float L_16 = __this->___m_Mean;
		V_2 = ((float)il2cpp_codegen_subtract(L_15, L_16));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:64>
		float L_17 = __this->___m_Mean;
		float L_18 = V_2;
		uint32_t L_19 = __this->___m_Count;
		__this->___m_Mean = ((float)il2cpp_codegen_add(L_17, ((float)(L_18/((float)((double)(uint32_t)L_19))))));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:65>
		float L_20 = V_1;
		float L_21 = __this->___m_Mean;
		V_3 = ((float)il2cpp_codegen_subtract(L_20, L_21));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:66>
		float L_22 = __this->___m_MeanDist2;
		float L_23 = V_2;
		float L_24 = V_3;
		__this->___m_MeanDist2 = ((float)il2cpp_codegen_add(L_22, ((float)il2cpp_codegen_multiply(L_23, L_24))));
	}

IL_00b7:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:68>
		return;
	}
}
IL2CPP_EXTERN_C  void RunningStatistics_AddDataPoint_m526C882C18A65D3923525ACC63C8A97896F297B4_AdjustorThunk (RuntimeObject* __this, uint32_t ___0_value, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	RunningStatistics_AddDataPoint_m526C882C18A65D3923525ACC63C8A97896F297B4(_thisAdjusted, ___0_value, method);
}
// Method Definition Index: 69385
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunningStatistics_Merge_m48B2C1B6723015C08D6152641882A7BC7923C677 (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 ___0_other, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	float V_1 = 0.0f;
	float V_2 = 0.0f;
	float V_3 = 0.0f;
	float V_4 = 0.0f;
	bool V_5 = false;
	bool V_6 = false;
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:80>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_0 = ___0_other;
		uint32_t L_1 = L_0.___m_Count;
		V_5 = (bool)((((int32_t)L_1) == ((int32_t)0))? 1 : 0);
		bool L_2 = V_5;
		if (!L_2)
		{
			goto IL_0015;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:81>
		goto IL_00f9;
	}

IL_0015:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:83>
		uint32_t L_3 = __this->___m_Count;
		V_6 = (bool)((((int32_t)L_3) == ((int32_t)0))? 1 : 0);
		bool L_4 = V_6;
		if (!L_4)
		{
			goto IL_0031;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:85>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_5 = ___0_other;
		*(RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*)__this = L_5;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:86>
		goto IL_00f9;
	}

IL_0031:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:89>
		uint32_t L_6 = __this->___m_Count;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_7 = ___0_other;
		uint32_t L_8 = L_7.___m_Count;
		V_0 = ((int32_t)il2cpp_codegen_add((int32_t)L_6, (int32_t)L_8));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:90>
		float L_9 = __this->___m_Mean;
		uint32_t L_10 = __this->___m_Count;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_11 = ___0_other;
		float L_12 = L_11.___m_Mean;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_13 = ___0_other;
		uint32_t L_14 = L_13.___m_Count;
		uint32_t L_15 = V_0;
		V_1 = ((float)(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(L_9, ((float)((double)(uint32_t)L_10)))), ((float)il2cpp_codegen_multiply(L_12, ((float)((double)(uint32_t)L_14))))))/((float)((double)(uint32_t)L_15))));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:93>
		float L_16 = __this->___m_Mean;
		float L_17 = V_1;
		V_2 = ((float)il2cpp_codegen_subtract(L_16, L_17));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:94>
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_18 = ___0_other;
		float L_19 = L_18.___m_Mean;
		float L_20 = V_1;
		V_3 = ((float)il2cpp_codegen_subtract(L_19, L_20));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:95>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:96>
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:97>
		uint32_t L_21 = __this->___m_Count;
		float L_22 = V_2;
		float L_23 = V_2;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_24 = ___0_other;
		uint32_t L_25 = L_24.___m_Count;
		float L_26 = V_3;
		float L_27 = V_3;
		uint32_t L_28 = __this->___m_Count;
		float L_29;
		L_29 = RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581(__this, NULL);
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_30 = ___0_other;
		uint32_t L_31 = L_30.___m_Count;
		float L_32;
		L_32 = RunningStatistics_get_Variance_m5955537CF0F189866C7FD9952CB094C014562581((&___0_other), NULL);
		V_4 = ((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_add(((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(((float)((double)(uint32_t)L_21)), L_22)), L_23)), ((float)il2cpp_codegen_multiply(((float)il2cpp_codegen_multiply(((float)((double)(uint32_t)L_25)), L_26)), L_27)))), ((float)il2cpp_codegen_multiply(((float)((double)(uint32_t)((int32_t)il2cpp_codegen_subtract((int32_t)L_28, 1)))), L_29)))), ((float)il2cpp_codegen_multiply(((float)((double)(uint32_t)((int32_t)il2cpp_codegen_subtract((int32_t)L_31, 1)))), L_32))));
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:99>
		uint32_t L_33 = V_0;
		__this->___m_Count = L_33;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:100>
		float L_34 = V_1;
		__this->___m_Mean = L_34;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:101>
		float L_35 = V_4;
		__this->___m_MeanDist2 = L_35;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:103>
		uint32_t L_36 = __this->___m_Minimum;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_37 = ___0_other;
		uint32_t L_38 = L_37.___m_Minimum;
		uint32_t L_39;
		L_39 = math_min_mFBB411A5384A9CFD7787E398A6F758553D3700A9_inline(L_36, L_38, NULL);
		__this->___m_Minimum = L_39;
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:104>
		uint32_t L_40 = __this->___m_Maximum;
		RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 L_41 = ___0_other;
		uint32_t L_42 = L_41.___m_Maximum;
		uint32_t L_43;
		L_43 = math_max_mD9D4307218A8CFA92F9C26871E508B23C17F6395_inline(L_40, L_42, NULL);
		__this->___m_Maximum = L_43;
	}

IL_00f9:
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:105>
		return;
	}
}
IL2CPP_EXTERN_C  void RunningStatistics_Merge_m48B2C1B6723015C08D6152641882A7BC7923C677_AdjustorThunk (RuntimeObject* __this, RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469 ___0_other, const RuntimeMethod* method)
{
	RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* _thisAdjusted;
	int32_t _offset = 1;
	_thisAdjusted = reinterpret_cast<RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469*>(__this + _offset);
	RunningStatistics_Merge_m48B2C1B6723015C08D6152641882A7BC7923C677(_thisAdjusted, ___0_other, method);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
// Method Definition Index: 69386
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void __JobReflectionRegistrationOutput__12582928334401220093_CreateJobReflectionData_mC48194201F255F2ADA41166D6839ADF412E2B950 (const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B_RuntimeMethod_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3_RuntimeMethod_var);
		s_Il2CppMethodInitialized = true;
	}
	il2cpp::utils::ExceptionSupportStack<RuntimeObject*, 1> __active_exceptions;
	try
	{
		IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C(IJobExtensions_EarlyJobInit_TisSendUpdate_t6166804EB8940870DB85A30D1B38CFB311ED41A7_mAC35E84733E91FDC0048DFD2A2A2E2E9D6DE964C_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24(IJobExtensions_EarlyJobInit_TisReceiveJob_t511C0226F40C94354B4FFEE828996B1B651AEA55_m36FA9B591F3FF91A4FA409773F8E862144D14C24_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048(IJobExtensions_EarlyJobInit_TisAnalyticsJob_t767EAAA36FBD84F0EF369E51C320B3EA3CD6B08C_mD53126E370801E13C1BA9D8F7BA1FCA08B2CD048_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B(IJobExtensions_EarlyJobInit_TisClearJob_t305217E05238D7ABEE4D58E4F1FCF69DED240055_m0A3CD3064D608E526219171C5DF3E7DC9DA5684B_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F(IJobExtensions_EarlyJobInit_TisReceiveJob_t5B9925DBAF30F7570D9B52B9F4A31FBEC596C3E7_m462236406BC73207F6D968438DE8FDACDFF5877F_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7(IJobExtensions_EarlyJobInit_TisSendJob_tF1A6B2697F6BE7F66468275482EF9F36308D5F23_mAD4E78B43C4F96C372F2BAE95AB7BB9D83ADC2A7_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8(IJobExtensions_EarlyJobInit_TisLogJob_t27DD5A165D8F7C880A5C6EE608AE25B4E2B3C50C_m0B8085230B0851D7670257D606A0D1C206B2F6F8_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90(IJobExtensions_EarlyJobInit_TisSendJob_t140A4BF3D4C4FB92CDA9959D4473D5058387A9CD_mAB1A5124A779A9E5AD1BD086295366B016C83B90_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8(IJobExtensions_EarlyJobInit_TisSendJob_t7FE82542C1C5BF90392BAF70DB34520A47AE9B41_mB75FE7BE37D205BBC198E92093E5EDA1B460CFA8_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF(IJobExtensions_EarlyJobInit_TisSimulatorReceiveJob_tF16E565AF6DC8E440CC9FFAD7C93C0D8686C2BE9_mC90FC7547E44854F3E4CEF62ACB09D733E1994EF_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B(IJobExtensions_EarlyJobInit_TisSimulatorSendJob_t01D59CE94E23B64CA9E6DB48CF7448150FEF973E_mBB10DCAF8B815091C93E9848E48CE93BA9190E8B_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B(IJobExtensions_EarlyJobInit_TisSendJob_tAD26D1F06724B9743F7DF6B072F0A058E08B9B09_mCDB54BEBB5785820AA3FF1CE2899783FA83CC74B_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A(IJobExtensions_EarlyJobInit_TisSendJob_t7AC0E2C0E02DC4D086B0E4D834F0849BF2A225F4_mA7816C2E1730AB4BBB7229507941DF9B6794188A_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34(IJobExtensions_EarlyJobInit_TisReceiveJob_t3228803953D2EBC29F0A3E3CCBB693FEC0879A0A_mAB339E6B08F0FC502C642D286E518E1B36A9BA34_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE(IJobExtensions_EarlyJobInit_TisReceiveJob_t3BC30EF16A064B4F89C510D40E5ED62E5A974373_mC2AD6B37AFA07620085AAEC60EB05423B3C2F6CE_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64(IJobExtensions_EarlyJobInit_TisSendJob_tF66446D9444624D914E486C9FE0D0F509C040E8C_mCDBB1F6E46764A5B1DFBF6D2D10759F4FC835B64_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6(IJobExtensions_EarlyJobInit_TisCompleteReceiveJob_t24F76EDC9987CDDD5FEFC9714E86886E9BCC1F44_m0F63DA017C69E4A3E10915062FCF4BDA2240E0B6_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0(IJobExtensions_EarlyJobInit_TisSendJob_t7DC6EFC8FD903F8462C7EC3552E4D253296089FD_mBAD728AF3070EAFD00E0C7EF0D3ABEAC8B3FD7C0_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E(IJobExtensions_EarlyJobInit_TisReceiveJob_tC59602B92BE9E3B77B543DC6C744945FC182F354_m7EF905C83291BF6739C8E38A493627E37F69DC7E_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3(IJobExtensions_EarlyJobInit_TisUpdateJob_tCDD60D50F9DE8EB1A63A58940105BBFF6A2E0216_mF7E7FFB1AA169F734FF8891758DEDA30962D4AB3_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543(IJobExtensions_EarlyJobInit_TisDequeuePacketsJob_tEC118B8F025C149EBB2813770D7C23C5E1CC960C_mF4CE89322419B7337B17779ECB99EBCEE2EE2543_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C(IJobExtensions_EarlyJobInit_TisFillBufferPointers_tD5A9CB3DAFFE5D2916534B730BEE9C442F12C2EB_mB9FCB9EC9AC1CBB6D7AC72C9E20EB146F6522B8C_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73(IJobExtensions_EarlyJobInit_TisReceiveJob_tF39E3B5C0851531A4800853F9F070CA1FC8D53D7_m4B2B8AC579515F7731091D81DD8474E49A35FE73_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E(IJobExtensions_EarlyJobInit_TisSendJob_tDA70F47DA90677D219E6F85A82F791AE21770ACD_m63BA8AD968961DA067DB74CBC6FFF3C7AF1CC12E_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A(IJobExtensions_EarlyJobInit_TisFlushSendJob_t98128C0A6D0D9B03F24F74243CA7024D16B4323B_m6FF978F3A6E766CB12F0EF9C8D081DA26D81CA3A_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D(IJobExtensions_EarlyJobInit_TisReceiveJob_tC5755FA7EC67985A0FDF0CD624F9695F03863BB6_mD5DFC4CD4FC9D22DD2C2037E1E12A8B9DB41BD3D_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B(IJobExtensions_EarlyJobInit_TisReceiveJob_1_tFA256E14CF25DE662E70FC2000714D5CF2D23E84_m43263823EF60E02AECA4B0B660C66639F7A5A09B_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8(IJobExtensions_EarlyJobInit_TisReceiveJob_1_tE0B78E6E6B13F46A46331EAA913CE48D89C1E0CD_m87F4DB106B1E094CC720E5C5E81C67676C3F64A8_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58(IJobExtensions_EarlyJobInit_TisReceiveJob_1_t67A762B59775096879E0588751B5CBA2E102D44C_m1783E077505A49A96536251AA07875EADFE7AC58_RuntimeMethod_var);
		IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC(IJobExtensions_EarlyJobInit_TisReceiveJob_1_t5FF0732537A394BE89DBBC7E7ADD9A4A01BCB04F_m9DA5E1E9986121E1F8909EB905D27DBC1761EBAC_RuntimeMethod_var);
		goto IL_00a7;
	}
	catch(Il2CppExceptionWrapper& e)
	{
		if(il2cpp_codegen_class_is_assignable_from (((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&Exception_t_il2cpp_TypeInfo_var)), il2cpp_codegen_object_class(e.ex)))
		{
			IL2CPP_PUSH_ACTIVE_EXCEPTION(e.ex);
			goto CATCH_009c;
		}
		throw e;
	}

CATCH_009c:
	{
		Exception_t* L_0 = ((Exception_t*)IL2CPP_GET_ACTIVE_EXCEPTION(Exception_t*));;
		il2cpp_codegen_runtime_class_init_inline(((RuntimeClass*)il2cpp_codegen_initialize_runtime_metadata_inline((uintptr_t*)&EarlyInitHelpers_tA67F29CEEF85CD33340F1A46E13686C44F97695A_il2cpp_TypeInfo_var)));
		EarlyInitHelpers_JobReflectionDataCreationFailed_mD6AB08D5BB411CCE38A87793C3C7062EC91FD1EC(L_0, NULL);
		IL2CPP_POP_ACTIVE_EXCEPTION(Exception_t*);
		goto IL_00a7;
	}

IL_00a7:
	{
		return;
	}
}
// Method Definition Index: 69387
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void __JobReflectionRegistrationOutput__12582928334401220093_EarlyInit_m9678F102715B35C437127FF27E7DCFF66FD517CD (const RuntimeMethod* method) 
{
	{
		__JobReflectionRegistrationOutput__12582928334401220093_CreateJobReflectionData_mC48194201F255F2ADA41166D6839ADF412E2B950(NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
// Method Definition Index: 69381
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float RunningStatistics_get_Mean_mD57820A85AFC03BAAD2979CD68C871C734ABA843_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:31>
		float L_0 = __this->___m_Mean;
		return L_0;
	}
}
// Method Definition Index: 69379
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Minimum_m0F877E31CE3F0AB13C9144765E968D969D0027CD_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:25>
		uint32_t L_0 = __this->___m_Minimum;
		return L_0;
	}
}
// Method Definition Index: 69380
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t RunningStatistics_get_Maximum_m7FBC60B3B0E8B874FFE089F0CD0011543F34FC53_inline (RunningStatistics_t5DC84BCF0B7EB895355412A033F9C3C2BE351469* __this, const RuntimeMethod* method) 
{
	{
		//<source_info:./Library/PackageCache/com.unity.transport@ed7eca02732f/Runtime/Analytics/RunningStatistics.cs:28>
		uint32_t L_0 = __this->___m_Maximum;
		return L_0;
	}
}
// Method Definition Index: 34904
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t math_lzcnt_m121BDDDEE89F5A401E2E5F0AD900D22E47C8741C_inline (uint32_t ___0_x, const RuntimeMethod* method) 
{
	LongDoubleUnion_tD71C400B6C4CD1A7F13CE8125AC6BBC7A22791CA V_0;
	memset((&V_0), 0, sizeof(V_0));
	bool V_1 = false;
	int32_t V_2 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5054>
		uint32_t L_0 = ___0_x;
		V_1 = (bool)((((int32_t)L_0) == ((int32_t)0))? 1 : 0);
		bool L_1 = V_1;
		if (!L_1)
		{
			goto IL_000e;
		}
	}
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5055>
		V_2 = ((int32_t)32);
		goto IL_0058;
	}

IL_000e:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5057>
		(&V_0)->___doubleValue = (0.0);
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5058>
		uint32_t L_2 = ___0_x;
		(&V_0)->___longValue = ((int64_t)il2cpp_codegen_add(((int64_t)4841369599423283200LL), ((int64_t)(uint64_t)((uint32_t)L_2))));
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5059>
		double* L_3 = (double*)(&(&V_0)->___doubleValue);
		double* L_4 = L_3;
		double L_5 = *((double*)L_4);
		*((double*)L_4) = (double)((double)il2cpp_codegen_subtract(L_5, (4503599627370496.0)));
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5060>
		LongDoubleUnion_tD71C400B6C4CD1A7F13CE8125AC6BBC7A22791CA L_6 = V_0;
		int64_t L_7 = L_6.___longValue;
		V_2 = ((int32_t)il2cpp_codegen_subtract(((int32_t)1054), ((int32_t)((int64_t)(L_7>>((int32_t)52))))));
		goto IL_0058;
	}

IL_0058:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:5061>
		int32_t L_8 = V_2;
		return L_8;
	}
}
// Method Definition Index: 34363
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR int32_t math_min_m02D43DF516544C279AF660EA4731449C82991849_inline (int32_t ___0_x, int32_t ___1_y, const RuntimeMethod* method) 
{
	int32_t V_0 = 0;
	int32_t G_B3_0 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:855>
		int32_t L_0 = ___0_x;
		int32_t L_1 = ___1_y;
		if ((((int32_t)L_0) < ((int32_t)L_1)))
		{
			goto IL_0008;
		}
	}
	{
		int32_t L_2 = ___1_y;
		G_B3_0 = L_2;
		goto IL_0009;
	}

IL_0008:
	{
		int32_t L_3 = ___0_x;
		G_B3_0 = L_3;
	}

IL_0009:
	{
		V_0 = G_B3_0;
		goto IL_000c;
	}

IL_000c:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:855>
		int32_t L_4 = V_0;
		return L_4;
	}
}
// Method Definition Index: 34714
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR float math_sqrt_mEF31DE7BD0179009683C5D7B0C58E6571B30CF4A_inline (float ___0_x, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		s_Il2CppMethodInitialized = true;
	}
	float V_0 = 0.0f;
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:3382>
		float L_0 = ___0_x;
		il2cpp_codegen_runtime_class_init_inline(Math_tEB65DE7CA8B083C412C969C92981C030865486CE_il2cpp_TypeInfo_var);
		double L_1;
		L_1 = sqrt(((double)((float)L_0)));
		V_0 = ((float)L_1);
		goto IL_000d;
	}

IL_000d:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:3382>
		float L_2 = V_0;
		return L_2;
	}
}
// Method Definition Index: 34367
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t math_min_mFBB411A5384A9CFD7787E398A6F758553D3700A9_inline (uint32_t ___0_x, uint32_t ___1_y, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	uint32_t G_B3_0 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:884>
		uint32_t L_0 = ___0_x;
		uint32_t L_1 = ___1_y;
		if ((!(((uint32_t)L_0) >= ((uint32_t)L_1))))
		{
			goto IL_0008;
		}
	}
	{
		uint32_t L_2 = ___1_y;
		G_B3_0 = L_2;
		goto IL_0009;
	}

IL_0008:
	{
		uint32_t L_3 = ___0_x;
		G_B3_0 = L_3;
	}

IL_0009:
	{
		V_0 = G_B3_0;
		goto IL_000c;
	}

IL_000c:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:884>
		uint32_t L_4 = V_0;
		return L_4;
	}
}
// Method Definition Index: 34385
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR uint32_t math_max_mD9D4307218A8CFA92F9C26871E508B23C17F6395_inline (uint32_t ___0_x, uint32_t ___1_y, const RuntimeMethod* method) 
{
	uint32_t V_0 = 0;
	uint32_t G_B3_0 = 0;
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:1016>
		uint32_t L_0 = ___0_x;
		uint32_t L_1 = ___1_y;
		if ((!(((uint32_t)L_0) <= ((uint32_t)L_1))))
		{
			goto IL_0008;
		}
	}
	{
		uint32_t L_2 = ___1_y;
		G_B3_0 = L_2;
		goto IL_0009;
	}

IL_0008:
	{
		uint32_t L_3 = ___0_x;
		G_B3_0 = L_3;
	}

IL_0009:
	{
		V_0 = G_B3_0;
		goto IL_000c;
	}

IL_000c:
	{
		//<source_info:./Library/PackageCache/com.unity.mathematics@19a9377c4ffa/Unity.Mathematics/math.cs:1016>
		uint32_t L_4 = V_0;
		return L_4;
	}
}
