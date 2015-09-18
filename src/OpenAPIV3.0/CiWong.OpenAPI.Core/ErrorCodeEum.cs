
namespace CiWong.OpenAPI.Core
{
	/// <summary>
	/// 二级错误码定义
	/// </summary>
	public enum ErrorCodeEum
	{
		/// <summary>
		/// 成功
		/// </summary>
		Success = 0,
		/// <summary>
		/// Http错误
		/// </summary>
		HttpError = 1,
		/// <summary>
		/// 服务被禁用
		/// </summary>
		ServiceDisabled = 101,
		/// <summary>
		/// 服务内部错误
		/// </summary>
		ServiceError = 102,
		/// <summary>
		/// 参数错误
		/// </summary>
		ArgumentFormatError = 201,
		/// <summary>
		/// 用户名或者密码
		/// </summary>
		UserAuthenticationError = 10004,

		#region 作业相关接口错误码定义与占位,具体错误详情,直接查找引用即可
		/// <summary>
		/// 作业错误码占位,以4701开始
		/// </summary>
		Work = 47,
		Work_4701 = 4701,
		Work_4702 = 4702,
		Work_4703 = 4703,
		Work_4704 = 4704,
		Work_4705 = 4705,
		Work_4706 = 4706,
		Work_4707 = 4707,
		Work_4708 = 4708,
		Work_4709 = 4709,
		Work_4710 = 4710,
		Work_4711 = 4711,
		#endregion

		#region 作业应用相关接口错误码定义与占位,具体错误详情,直接查找引用即可
		/// <summary>
		/// 作业应用错误码占位,以4801开始
		/// </summary>
		ExpandWork = 48,
		ExpandWork_4801 = 4801,
		ExpandWork_4802 = 4802,
		ExpandWork_4803 = 4803,
		ExpandWork_4804 = 4804,
		ExpandWork_4805 = 4805,
		ExpandWork_4806 = 4806,
		ExpandWork_4807 = 4807,
		ExpandWork_4808 = 4808,
		ExpandWork_4809 = 4809,
		ExpandWork_4810 = 4810,
		ExpandWork_4811 = 4811,
		ExpandWork_4812 = 4812,
		ExpandWork_4813 = 4813,
		ExpandWork_4814 = 4814,
		ExpandWork_4815 = 4815,
		ExpandWork_4816 = 4816,
		ExpandWork_4817 = 4817,
		ExpandWork_4818 = 4818,
		ExpandWork_4819 = 4819,
		ExpandWork_4820 = 4820,
		ExpandWork_4821 = 4821,
		ExpandWork_4822 = 4822,
		ExpandWork_4823 = 4823,
		ExpandWork_4824 = 4824,
		ExpandWork_4825 = 4825,
		ExpandWork_4826 = 4826,
		ExpandWork_4827 = 4827,
		ExpandWork_4828 = 4828,
		ExpandWork_4829 = 4829,
		ExpandWork_4830 = 4830,
		ExpandWork_4831 = 4831,
		ExpandWork_4832 = 4832,
		#endregion

		#region 书柜相关接口错误码定义与占位,具体错误详情,直接查找引用即可
		/// <summary>
		/// 书柜错误码占位,以4901开始
		/// </summary>
		BookCase = 49,
		BookCase_4901 = 4901,
		BookCase_4902 = 4902,
		BookCase_4903 = 4903,
		BookCase_4904 = 4904,
		#endregion

		#region 资源相关接口错误码定义与占位,具体错误详情,直接查找引用即可
		/// <summary>
		/// 资源错误码占位,以5001开始
		/// </summary>
		Resource = 50,
		Resource_5001 = 5001,
		Resource_5002 = 5002,
		Resource_5003 = 5003,
		Resource_5004 = 5004,
		Resource_5005 = 5005,
		/// <summary>
		/// 未找到指定的资源包或者资源包格式不正确
		/// </summary>
		Resource_5006 = 5006,
		/// <summary>
		/// 未找到指定的二维码
		/// </summary>
		Resource_5007 = 5007,
		/// <summary>
		/// 二维码数据格式错误
		/// </summary>
		Resource_5008 = 5008,
		/// <summary>
		/// 二维码尚未填充资源
		/// </summary>
		Resource_5009 = 5009,
		/// <summary>
		/// 当前二维码尚未生成离线包
		/// </summary>
		Resource_5010 = 5010,
		/// <summary>
		/// 当前目录不存在
		/// </summary>
		Resource_5011 = 5011,
		/// <summary>
		/// 当前URL信息系统不支持
		/// </summary>
		Resource_5012 = 5012,
		#endregion

		#region 资源相关接口错误码定义与占位,具体错误详情,直接查找引用即可
		Agent = 52,
		Agent_5201 = 5201
		#endregion
	}
}
