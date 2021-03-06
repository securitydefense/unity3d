#region Using
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
using UnityEngine.Networking;
#endif
using System.Runtime.InteropServices;

#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
using UnityEngine.EventSystems;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
#endregion

/// <summary>
/// SDK接口类
/// </summary>
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public sealed class RiseSdk {
#if UNITY_ANDROID
    private static RiseSdk _instance = null;
    private AndroidJavaClass _class = null;
    private bool paymentSystemValid = false;

    private String BACK_HOME_ADPOS = M_CUSTOM;
    private bool BACK_HOME_AD_ENABLE = false;
    private double BACK_HOME_AD_TIME = 0;
    private bool canShowBackHomeAd = false;

    /*
    public const int SERVER_RESULT_RECEIVE_GAME_DATA = 1;
    public const int SERVER_RESULT_SAVE_USER_DATA = 2;
    public const int SERVER_RESULT_RECEIVE_USER_DATA = 3;
    public const int SERVER_RESULT_VERIFY_CODE = 4;
    public const int SERVER_RESULT_SALES_CLICK = 5;
    */

    /// <summary>
    /// 配置计费系统的可用状态，SDK自动调用。
    /// </summary>
    /// <param name="valid">要配置的状态</param>
    public void SetPaymentSystemValid (bool valid) {
        paymentSystemValid = valid;
    }

    /// <summary>
    /// 单例对象。
    /// </summary>
    public static RiseSdk Instance {
        get {
            if (null == _instance)
                _instance = new RiseSdk ();
            return _instance;
        }
    }

    static RiseSdk () {
#if UNITY_EDITOR
        EditorApplication.update -= SetExternalSDCard;
        EditorApplication.update += SetExternalSDCard;
        UnityEditor.PlayerSettings.Android.forceSDCardPermission = true;
#endif
    }

#if UNITY_EDITOR
    private static void SetExternalSDCard () {
        if (!EditorApplication.isPlaying) {
            EditorApplication.update -= SetExternalSDCard;
            UnityEditor.PlayerSettings.Android.forceSDCardPermission = true;
        }
    }
#endif

    /// <summary>
    /// 初始化SDK，最好在第一个场景加载时初始化。
    /// </summary>
    public void Init () {
        RiseEditorAd.hasInit = true;
        if (_class != null) {
            return;
        }
#if UNITY_ANDROID
        try {
            RiseSdkListener.Instance.enabled = true;
            _class = new AndroidJavaClass ("com.android.client.Unity");
            if (_class != null) {
                AndroidJNIHelper.debug = true;
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                    using (AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject> ("currentActivity")) {
                        _class.CallStatic ("onCreate", context);
                    }
                }
            }
        } catch (Exception e) {
            Debug.LogWarning (e.StackTrace);
            _class = null;
        } finally {
            lruCache = new FileLRUCache (20);
        }
#endif
    }

    public int GetScreenWidth () {
        return Screen.width;
    }

    public int GetScreenHeight () {
        return Screen.height;
    }

    /// <summary>
    /// 显示bannner广告。
    /// 如果有需要可以添加bannner广告被点击的回调：
    /// RiseSdkListener.OnAdEvent += 
    /// (
    /// RiseSdk.AdEventType type//广告事件类型，需要判断是否为RiseSdk.AdEventType.BannerAdClicked
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="tag">bannner广告tag</param>
    /// <param name="pos">bannner显示的位置，如：POS_BANNER_MIDDLE_BOTTOM为在底部居中显示</param>
    public void ShowBanner (string tag, int pos) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowBanner (tag, pos);
#endif
        if (_class != null) {
            _class.CallStatic ("showBanner", tag, pos);
            Debug.LogWarning ("showBanner");
        }
    }

    /// <summary>
    /// 显示bannner广告。
    /// 如果有需要可以添加bannner广告被点击的回调：
    /// RiseSdkListener.OnAdEvent += 
    /// (
    /// RiseSdk.AdEventType type//广告事件类型，需要判断是否为RiseSdk.AdEventType.BannerAdClicked
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="pos">bannner显示的位置，如：POS_BANNER_MIDDLE_BOTTOM为在底部居中显示</param>
    public void ShowBanner (int pos) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowBanner (pos);
#endif
        if (_class != null) {
            _class.CallStatic ("showBanner", pos);
            Debug.LogWarning ("showBanner");
        }
    }

    /// <summary>
    /// 关闭banner广告。
    /// </summary>
    public void CloseBanner () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.CloseBanner ();
#endif
        if (_class != null) {
            _class.CallStatic ("closeBanner");
        }
    }

    /// <summary>
    /// 显示大屏广告。
    /// 如果有需要可以添加大屏广告被点击和被关闭的回调：
    /// RiseSdkListener.OnAdEvent += 
    /// (
    /// RiseSdk.AdEventType type//广告事件类型，需要判断是否为
    /// //RiseSdk.AdEventType.FullAdClosed（大屏广告被关闭）
    /// //或RiseSdk.AdEventType.FullAdClicked（大屏广告被点击）
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="tag">大屏广告弹出时机，如：M_PAUSE为游戏暂停时弹出</param>
    public void ShowAd (String tag) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowAd (tag);
#endif
        if (_class != null) {
            _class.CallStatic ("showFullAd", tag);
        }
    }

    /// <summary>
    /// 更多游戏接口，跳到推广的游戏列表界面。
    /// </summary>
    public void ShowMore () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("ShowMore");
#endif
        if (_class != null) {
            _class.CallStatic ("moreGame");
        }
    }

    /// <summary>
    /// 检测视频广告是否加载完成。
    /// </summary>
    /// <returns>true完成，false失败</returns>
    public bool HasRewardAd () {
#if UNITY_EDITOR
        return true;
#endif
        if (_class != null) {
            return _class.CallStatic<bool> ("hasRewardAd");
        }
        return false;
    }

    /// <summary>
    /// 检测视频广告是否加载完成。
    /// </summary>
    /// <param name="tag">视频广告tag</param>
    /// <returns>true完成，false失败</returns>
    public bool HasRewardAd (string tag) {
#if UNITY_EDITOR
        return true;
#endif
        if (_class != null) {
            return _class.CallStatic<bool> ("hasRewardAd", tag);
        }
        return false;
    }

    /// <summary>
    /// 显示视频广告。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnRewardAdEvent += 
    /// (
    /// RiseSdk.AdEventType result,//是否成功显示视频广告，成功返回RiseSdk.AdEventType.RewardAdShowFinished，失败返回RiseSdk.AdEventType.RewardAdShowFailed
    /// int rewardId//视频广告调用时机
    /// ) 
    /// => {to do something};
    /// 如果有需要可以添加视频广告被关闭的回调：
    /// RiseSdkListener.OnAdEvent += 
    /// (
    /// RiseSdk.AdEventType type//广告事件类型，需要判断是否为RiseSdk.AdEventType.VideoAdClosed
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="rewardId">客户端自己配置的视频广告调用时机</param>
    public void ShowRewardAd (int rewardId) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowRewardAd (rewardId);
#endif
        if (_class != null) {
            _class.CallStatic ("showRewardAd", rewardId);
        }
    }

    /// <summary>
    /// 显示视频广告。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnRewardAdEvent += 
    /// (
    /// RiseSdk.AdEventType result,//是否成功显示视频广告，成功返回RiseSdk.AdEventType.RewardAdShowFinished，失败返回RiseSdk.AdEventType.RewardAdShowFailed
    /// int rewardId，//视频广告调用时机
    /// string tag//后台配置的视频广告位置tag
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="tag">后台配置的视频广告位置tag</param>
    /// <param name="rewardId">客户端自己配置的视频广告调用时机</param>
    public void ShowRewardAd (string tag, int rewardId) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowRewardAd (tag, rewardId);
#endif
        if (_class != null) {
            _class.CallStatic ("showRewardAd", tag, rewardId);
        }
    }

    public void enableBackHomeAd (bool enabled, String adPos) {
        BACK_HOME_ADPOS = adPos;
        BACK_HOME_AD_ENABLE = enabled;
    }

    /// <summary>
    /// 游戏获得焦点，SDK自动调用。
    /// </summary>
    public void OnResume () {
        if (_class != null) {
            _class.CallStatic ("onResume");
        }
        if (BACK_HOME_AD_ENABLE) {
            if (canShowBackHomeAd && BACK_HOME_AD_TIME <= 0) {
                canShowBackHomeAd = false;
                ShowAd (BACK_HOME_ADPOS);
            }
        }
    }

    /// <summary>
    /// 游戏失去焦点，SDK自动调用。
    /// </summary>
    public void OnPause () {
        if (_class != null) {
            _class.CallStatic ("onPause");
        }
        if (BACK_HOME_AD_ENABLE) {
            double now = GetCurrentTimeInMills ();
            double delta = now - BACK_HOME_AD_TIME;
            canShowBackHomeAd = delta > 2000;
            if (canShowBackHomeAd) {
                BACK_HOME_AD_TIME = 0;
            }
        }
    }

    /// <summary>
    /// 游戏打开，SDK自动调用。
    /// </summary>
    public void OnStart () {
        if (_class != null) {
            _class.CallStatic ("onStart");
        }

    }

    /// <summary>
    /// 游戏退出，SDK自动调用。
    /// </summary>
    public void OnStop () {
        if (_class != null) {
            _class.CallStatic ("onStop");
        }
    }

    /// <summary>
    /// 游戏销毁，SDK自动调用。
    /// </summary>
    public void OnDestroy () {
        if (_class != null) {
            _class.CallStatic ("onDestroy");
        }
    }

    /// <summary>
    /// 需要退出游戏时调用。
    /// </summary>
    public void OnExit () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.OnExit ();
#endif
        if (_class != null) {
            _class.CallStatic ("onQuit");
        }
    }

    /// <summary>
    /// 检测计费点是否存在。
    /// </summary>
    /// <param name="billingId">计费点id</param>
    public void HasPaid (int billingId) {
        if (_class != null) {
            _class.CallStatic ("query", billingId);
        }
    }

    /// <summary>
    /// 检测计费是否可用。
    /// </summary>
    /// <returns>true可用，false不可用</returns>
    public bool IsPayEnabled () {
        return paymentSystemValid;
    }

    /// <summary>
    /// 支付接口，对计费点进行支付。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnPaymentEvent += 
    /// (
    /// int result,//计费结果，如：PAYMENT_RESULT_SUCCESS为计费成功
    /// int billId//计费点id
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="billingId">计费点id</param>
    public void Pay (int billingId) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Pay (billingId);
#endif
        if (_class != null) {
            _class.CallStatic ("pay", billingId);
        }
    }

    /// <summary>
    /// 分享游戏。
    /// </summary>
    public void Share () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Share");
#endif
        if (_class == null) {
            return;
        }
        _class.CallStatic ("share");
    }

    /// <summary>
    /// 获取自定义json数据，该接口最好在SDK初始化完成后3秒或以上再调用。
    /// </summary>
    /// <returns>返回后台配置的自定义json数据，如：{"x":"x", "x":8, "x":{x}, "x":[x]}</returns>
    public string GetExtraData () {
        if (_class == null) {
            return null;
        }
        return _class.CallStatic<string> ("getExtraData");
    }

    /// <summary>
    /// 统计数据到Google Analytics。
    /// </summary>
    /// <param name="category">需要统计的数据分类名称</param>
    /// <param name="action">需要统计的数据属性名称</param>
    /// <param name="label">数据的属性值</param>
    /// <param name="value">一般传0</param>
    public void TrackEvent (string category, string action, string label, int value) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Track: " + category + ", action: " + action + ", label: " + label + ", value: " + value);
#endif
        if (_class == null) {
            return;
        }
        _class.CallStatic ("trackEvent", category, action, label, value);
    }

    /// <summary>
    /// 跳转到游戏评分界面。
    /// </summary>
    public void Rate () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Rate");
#endif
        if (_class == null) {
            return;
        }
        _class.CallStatic ("rate");
    }

    /// <summary>
    /// 显示Native广告，暂时不对外开放。
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="yPercent"></param>
    public void ShowNativeAd (string tag, int yPercent) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("ShowNativeAd");
#endif
        if (_class != null) {
            _class.CallStatic ("showNative", tag, yPercent);
        }
    }

    /// <summary>
    /// 隐藏Native广告。
    /// </summary>
    /// <param name="tag">广告tag</param>
    public void HideNativeAd (string tag) {
        if (_class != null) {
            _class.CallStatic ("hideNative", tag);
        }
    }

    /// <summary>
    /// 检测Native广告是否加载成功。
    /// </summary>
    /// <param name="tag">广告tag</param>
    /// <returns>true成功， false失败</returns>
    public bool HasNativeAd (string tag) {
        if (_class != null) {
            return _class.CallStatic<bool> ("hasNative", tag);
        } else {
            return false;
        }
    }

    /// <summary>
    /// 登陆faceboook账户。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnSNSEvent += 
    /// (
    /// bool success,//登陆是否成功
    /// int eventType,//事件类型，需要判断是否为SNS_EVENT_LOGIN
    /// int data//无数据返回
    /// ) 
    /// => {to do something};
    /// </summary>
    public void Login () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Login");
#endif
        if (_class != null) {
            _class.CallStatic ("login");
        }
    }

    /// <summary>
    /// 检测faceboook是否已经登陆。
    /// </summary>
    /// <returns>true已登陆， false未登陆</returns>
    public bool IsLogin () {
        if (_class != null) {
            return _class.CallStatic<bool> ("isLogin");
        } else {
            return false;
        }
    }

    /// <summary>
    /// 登出faceboook账户。
    /// </summary>
    public void Logout () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Logout");
#endif
        if (_class != null) {
            _class.CallStatic ("logout");
        }
    }

    /// <summary>
    /// 邀请faceboook好友安装游戏。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnSNSEvent += 
    /// (
    /// bool success,//发送邀请是否成功
    /// int eventType,//事件类型，这里需要判断是否为SNS_EVENT_INVITE
    /// int data//无数据返回
    /// ) 
    /// => {to do something};
    /// </summary>
    public void Invite () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Invite");
#endif
        if (_class != null) {
            _class.CallStatic ("invite");
        }
    }

    /// <summary>
    /// 向faceboook好友发起挑战。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnSNSEvent += 
    /// (
    /// bool success,//发送挑战是否成功
    /// int eventType,//事件类型，这里需要判断是否为SNS_EVENT_CHALLENGE
    /// int data//成功挑战的好友数量
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="title">挑战标题</param>
    /// <param name="message">挑战内容</param>
    public void Challenge (string title, string message) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Challenge, title: " + title + ", message: " + message);
#endif
        if (_class != null) {
            _class.CallStatic ("challenge", title, message);
        }
    }

    /// <summary>
    /// 获取我的faceboook个人信息。
    /// </summary>
    /// <returns>
    /// 返回json数据，格式为：
    /// {
    /// "id":"0000000000000000",//我的facebook账户id
    /// "name":"Me is me",//我的facebook账户名称 
    /// "picture":"/data/empty_not_exists"//我的facebook账户个人图片本地保存的绝对路径
    /// }
    /// </returns>
    public string Me () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Me");
#endif
        if (_class != null) {
            return _class.CallStatic<string> ("me");
        } else {
            return "{}";
        }
    }

    /// <summary>
    /// 获取faceook朋友信息列表。
    /// </summary>
    /// <returns>
    /// 返回json数据，格式为：
    /// [
    /// {
    /// "id":"0000000000000001",//我的facebook好友1的账户id
    /// "name":"Friend 1",//我的facebook好友1的账户名称
    /// "picture":"/data/empty_not_exists1"//我的facebook好友1个人头像本地保存的绝对路径
    /// },
    /// {
    /// "id":"0000000000000002",//我的facebook好友2的账户id
    /// "name":"Friend 2",//我的facebook好友2的账户名称
    /// "picture":"/data/empty_not_exists2"//我的facebook好友2个人头像本地保存的绝对路径
    /// },
    /// {
    /// "id":"0000000000000003",//我的facebook好友3的账户id
    /// "name":"Friend 3",//我的facebook好友3的账户名称
    /// "picture":"/data/empty_not_exists3"//我的facebook好友3个人头像本地保存的绝对路径
    /// }
    /// ]
    /// </returns>
    public string GetFriends () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("GetFriends");
#endif
        if (_class != null) {
            return _class.CallStatic<string> ("friends");
        } else {
            return "[]";
        }
    }

    /// <summary>
    /// facebook点赞接口。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnSNSEvent += 
    /// (
    /// bool success,//点赞是否成功
    /// int eventType,//事件类型，这里需要判断是否为SNS_EVENT_LIKE
    /// int data//无数据返回
    /// ) 
    /// => {to do something};
    /// </summary>
    public void Like () {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Like");
#endif
        if (_class != null) {
            _class.CallStatic ("like");
        }
    }

    public string GetMePictureURL () {
#if UNITY_EDITOR
        return "http://img.qq1234.org/uploads/allimg/141205/3_141205195713_3.jpg";
#else
        string me = Me ();
        string meImgPath = "http://img.qq1234.org/uploads/allimg/141205/3_141205195713_3.jpg";
        if (!string.IsNullOrEmpty (me) && me.Length > 5) {
            Dictionary<string, object> meData = (Dictionary<string, object>) RiseJson.Deserialize (me);
            if (meData.ContainsKey ("picture")) {
                meImgPath = meData["picture"].ToString ();
            }
        }
        return meImgPath;
#endif
    }

    /// <summary>
    /// 获取后台配置的內付计费点信息，价格单位和价格
    /// </summary>
    /// <returns>
    /// 返回json数据，格式为：
    /// {
    /// "currency":"USD",//价格单位
    /// "1":"0.99",//计费点价格 
    /// "2":"1.99"//计费点价格
    /// }
    /// </returns>
    public string GetPaymentsPrices () {
#if UNITY_EDITOR
        return "{}";
#else
        if (_class != null) {
            return _class.CallStatic<string> ("getPrices");
        } else {
            return "{}";
        }
#endif
    }

    /// <summary>
    /// 获取后台配置信息和Unity无法直接获取的一些信息。
    /// </summary>
    /// <param name="configId">需要获取的信息类型，如：CONFIG_KEY_VERSION_CODE为获取游戏的版本号</param>
    /// <returns>返回请求的信息</returns>
    public string GetConfig (int configId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("GetConfig, configId: " + configId);
#endif
        if (_class != null) {
            return _class.CallStatic<string> ("getConfig", configId);
        } else {
            return "0";
        }
    }

    /// <summary>
    /// 下载文件，该接口无回调方法，直接返回文件保存路径。
    /// </summary>
    /// <param name="url">需要下载的文件的url地址</param>
    /// <returns>返回该文件保存在本地的绝对路径</returns>
    public string CacheUrl (string url) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("CacheUrl, url: " + url);
#endif
        if (_class != null) {
            return _class.CallStatic<string> ("cacheUrl", url);
        } else {
            return "";
        }
    }

    /// <summary>
    /// 下载文件。
    /// 该接口的回调方法为：
    /// RiseSdkListener.OnCacheUrlResult += 
    /// (
    /// bool success,//下载是否成功
    /// int tag,//客户端添加的tag
    /// string data//文件保存在本地的绝对路径
    /// ) 
    /// => {to do something};
    /// </summary>
    /// <param name="tag">客户端添加的tag</param>
    /// <param name="url">需要下载的文件的url地址</param>
    public void CacheUrl (int tag, string url) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("CacheUrl, tag: " + tag + ", url: " + url);
#endif
        if (_class != null) {
            _class.CallStatic ("cacheUrl", tag, url);
        }
    }

    /// <summary>
    /// 检测本机是否安装了app。
    /// </summary>
    /// <param name="packageName">需要检测的app的包名</param>
    /// <returns>true已安装， false未安装</returns>
    public bool HasApp (string packageName) {
        if (_class == null) {
            return false;
        } else {
            return _class.CallStatic<bool> ("hasApp", packageName);
        }
    }

    /// <summary>
    /// 打开本机上已安装的app。
    /// </summary>
    /// <param name="packageName">需要打开的app的包名</param>
    public void LaunchApp (string packageName) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
        if (_class != null) {
            _class.CallStatic ("launchApp", packageName);
        }
    }

    /// <summary>
    /// 前往商店下载app。
    /// </summary>
    /// <param name="packageName">需要下载的app的包名</param>
    public void GetApp (string packageName) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
        if (_class != null) {
            _class.CallStatic ("getApp", packageName);
        }
    }

    /// <summary>
    /// 获取指定app的后台配置信息和Unity无法直接获取的一些信息。
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="configId">需要获取的信息类型，如：CONFIG_KEY_VERSION_CODE为获取游戏的版本号</param>
    /// <returns>返回请求的信息</returns>
    public string GetConfig (string packageName, int configId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("GetConfig, packageName: " + packageName + ", configId: " + configId);
#endif
        if (_class != null) {
            return _class.CallStatic<string> ("getConfig", packageName, configId);
        } else {
            return "";
        }
    }

    /// <summary>
    /// 系统的确认对话框。
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="message">对话框内容</param>
    public void Alert (string title, string message) {
        BACK_HOME_AD_TIME = GetCurrentTimeInMills ();
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Alert (title, message);
#endif
        if (_class != null) {
            _class.CallStatic ("alert", title, message);
        }
    }

    /// <summary>
    /// 系统的toast提示信息。
    /// </summary>
    /// <param name="message">提示内容</param>
    public void Toast (string message) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast (message);
#endif
        if (_class != null) {
            _class.CallStatic ("toast", message);
        }
    }

    /// <summary>
    /// 检测网络是否可用。
    /// </summary>
    /// <returns>true可用， false不可用</returns>
    public bool IsNetworkConnected () {
#if UNITY_EDITOR
        return true;
#endif
        if (_class != null) {
            return _class.CallStatic<bool> ("isNetworkConnected");
        }
        return true;
    }

    #region Umeng

    /// <summary>
    /// 友盟统计，设置玩家等级。
    /// </summary>
    /// <param name="level">等级</param>
    public void UM_setPlayerLevel (int level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, setPlayerLevel: " + level);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_setPlayerLevel", level);
        }
    }

    /// <summary>
    /// 友盟统计，自定义事件统计。
    /// </summary>
    /// <param name="eventId">事件id，要与在友盟后台添加的保持一致</param>
    public void UM_onEvent (String eventId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onEvent: " + eventId);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_onEvent", eventId);
        }
    }

    /// <summary>
    /// 友盟统计，自定义事件统计。
    /// </summary>
    /// <param name="eventId">事件id，要与在友盟后台添加的保持一致</param>
    /// <param name="eventLabel">事件标签</param>
    public void UM_onEvent (String eventId, String eventLabel) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onEvent: " + eventId + ", label: " + eventLabel);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_onEvent", eventId, eventLabel);
        }
    }

    /// <summary>
    /// 友盟统计，自定义事件统计。
    /// </summary>
    /// <param name="eventId">事件id，要与在友盟后台添加的保持一致</param>
    /// <param name="mapStr">要统计的事件标签的键值对</param>
    public void UM_onEventValue (string eventId, Dictionary<string, string> mapStr) {
        if (_class != null) {
            AndroidJavaObject map = null;
            if (mapStr != null) {
                try {
                    map = new AndroidJavaObject ("java.util.Map");
                    foreach (KeyValuePair<string, string> pair in mapStr) {
                        map.Call<string> ("put", pair.Key, pair.Value);
                    }
                } catch (System.Exception ex) {
                    Debug.LogError ("UM_onEventValue Exception msg:\n" + ex.StackTrace);
                }
            }
            _class.CallStatic ("UM_onEventValue", map, 1);
        }
    }

    /// <summary>
    /// 友盟统计，进入某页面。
    /// </summary>
    /// <param name="pageName">页面名称</param>
    public void UM_onPageStart (String pageName) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onPageStart: " + pageName);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_onPageStart", pageName);
        }
    }

    /// <summary>
    /// 友盟统计，离开某页面。
    /// </summary>
    /// <param name="pageName">页面名称</param>
    public void UM_onPageEnd (String pageName) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onPageEnd: " + pageName);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_onPageEnd", pageName);
        }
    }

    /// <summary>
    /// 友盟统计，关卡开始。
    /// </summary>
    /// <param name="level">关卡名称</param>
    public void UM_startLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, startLevel: " + level);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_startLevel", level);
        }
    }

    /// <summary>
    /// 友盟统计，关卡失败。
    /// </summary>
    /// <param name="level">关卡名称</param>
    public void UM_failLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, failLevel: " + level);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_failLevel", level);
        }
    }

    /// <summary>
    /// 友盟统计，关卡胜利或结束。
    /// </summary>
    /// <param name="level">关卡名称</param>
    public void UM_finishLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, finishLevel: " + level);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_finishLevel", level);
        }
    }

    /// <summary>
    /// 友盟统计，游戏内付统计。
    /// </summary>
    /// <param name="money">内付的金额</param>
    /// <param name="itemName">内付购买的商品名称</param>
    /// <param name="number">内付购买的商品数量</param>
    /// <param name="price">内付购买的商品价格</param>
    public void UM_pay (double money, String itemName, int number, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, pay, money: " + money + ", item: " + itemName + ", number: " + number + ", price: " + price);
#endif
        /**
         * 最后一个参数是支付渠道：
         * 1    Google Play
         * 2    支付宝
         * 3    网银
         * 4    财付通
         * 5    移动通信
         * 6    联通通信
         * 7    电信通信
         * 8    paypal
         */
        if (_class != null) {
            _class.CallStatic ("UM_pay", money, itemName, number, price);
        }
    }

    /// <summary>
    /// 友盟统计，购买道具统计。
    /// </summary>
    /// <param name="itemName">购买游戏中道具名称</param>
    /// <param name="count">购买道具数量</param>
    /// <param name="price">购买道具价格</param>
    public void UM_buy (String itemName, int count, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, buy, item: " + itemName + ", count: " + count + ", price: " + price);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_buy", itemName, count, price);
        }
    }

    /// <summary>
    /// 友盟统计，使用道具统计。
    /// </summary>
    /// <param name="itemName">使用道具名称</param>
    /// <param name="number">使用道具数量</param>
    /// <param name="price">使用道具价格</param>
    public void UM_use (String itemName, int number, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, use, item: " + itemName + ", number: " + number + ", price: " + price);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_use", itemName, number, price);
        }
    }

    /// <summary>
    /// 友盟统计，额外奖励统计。
    /// </summary>
    /// <param name="itemName">奖励道具名称</param>
    /// <param name="number">奖励道具数量</param>
    /// <param name="price">奖励道具价格</param>
    /// <param name="trigger">触发奖励的事件, 取值在 1~10 之间，“1”已经被预先定义为“系统奖励”， 2~10 需要在网站设置含义</param>
    public void UM_bonus (String itemName, int number, double price, int trigger) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, bonus, item: " + itemName + ", number: " + number + ", price: " + price + ", trigger: " + trigger);
#endif
        if (_class != null) {
            _class.CallStatic ("UM_bonus", itemName, number, price, trigger);
        }
    }

    #endregion

    /*
	public void SubmitScore(string leaderBoardId, long score, string extra) {
		if (_class != null) {
			_class.CallStatic ("submitScore", leaderBoardId, score, extra);
		}
	}

	public void LoadFriendLeaderBoard(string leaderBoardId, int start, int end, string friends) {
		if (_class != null) {
			_class.CallStatic ("loadLeaderBoard", leaderBoardId, start, end, friends);
		}
	}

	public void LoadGlobalLeaderBoard(string leaderBoardId, int start, int end) {
		if (_class != null) {
			_class.CallStatic ("loadGlobalLeaderBoard", leaderBoardId, start, end);
		}
	}

	public void LoadGameData(int version) {
		if (_class != null) {
			_class.CallStatic ("loadExtra", version);
		}
	}

	public void LoadUserData() {
		if (_class != null) {
			_class.CallStatic ("loadData");
		}
	}

	public void SaveUserData(string data) {
		if (_class != null) {
			_class.CallStatic ("saveData", data);
		}
	}

	public void VerifyCode(string code) {
		if (_class != null) {
			_class.CallStatic ("verifyCode", code);
		}
	}

	public void ShowSales(int saleId) {
		if (_class != null) {
			_class.CallStatic ("showSales", saleId);
		}
	}*/

    #region Android_Dummy
    public string GetMeFirstName () {
#if UNITY_EDITOR
        return "FirstName";
#else
        return "FirstName";
#endif
    }

    public string GetMeLastName () {
#if UNITY_EDITOR
        return "LastName";
#else
        return "LastName";
#endif
    }

    public string GetMeId () {
#if UNITY_EDITOR
        return "MeId";
#else
        return "MeId";
#endif
    }

    public string GetMeName () {
#if UNITY_EDITOR
        return "MeName";
#else
        return "MeName";
#endif
    }

    public void FetchFriends (bool invitable) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("FetchFriends");
#endif
    }

    public void FetchScores () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("FetchScores");
#endif
    }

    public void Share (string contentURL, string tag, string quote) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Share: " + contentURL + ", " + tag + ", " + quote);
#endif
    }

    public void RestorePayments () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("RestorePayments");
#endif
    }

    public void SdkLog (string message) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("sdk log: " + message);
#endif
    }

    public void LoadAd (string tag) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("LoadAd: " + tag);
#endif
    }
    #endregion

#elif UNITY_IOS
    [DllImport ("__Internal")]
    private static extern int getScreenWidth ();
    [DllImport ("__Internal")]
    private static extern int getScreenHeight ();
    [DllImport ("__Internal")]
    private static extern void onCreate ();
    [DllImport ("__Internal")]
    private static extern void showBanner (int pos);
    [DllImport ("__Internal")]
    private static extern void showBannerWithTag (string tag, int pos);
    [DllImport ("__Internal")]
    private static extern void closeBanner ();
    [DllImport ("__Internal")]
    private static extern bool isVideoAvaliable ();
    [DllImport ("__Internal")]
    private static extern bool isVideoAvaliableWithTag (string tag);
    [DllImport ("__Internal")]
    private static extern void showRewardVideo (int placementId);
    [DllImport ("__Internal")]
    private static extern void showRewardVideoWithTag (string tag, int placementId);
    [DllImport ("__Internal")]
    private static extern void showInterstitialAd (string tag);
    [DllImport ("__Internal")]
    private static extern void showInterstitialAdWithTag (string tag, int delayShowSeconds);
    [DllImport ("__Internal")]
    private static extern void showInterstitialAdWithTag2 (string tag, int delayShowSeconds, double delayTimeInterval);
    [DllImport ("__Internal")]
    private static extern void showIconAd (float width, float xPercent, float yPercent);
    [DllImport ("__Internal")]
    private static extern void loadInterstitialAd(string tag);
    [DllImport ("__Internal")]
    private static extern void closeIconAd ();
    [DllImport ("__Internal")]
    private static extern void rateInApp ();
    [DllImport ("__Internal")]
    private static extern bool isNetworkAvaliable ();
    [DllImport ("__Internal")]
    private static extern void pay (int billingId);
    [DllImport ("__Internal")]
    private static extern void isSubscriptionActive (int billingId);
    [DllImport ("__Internal")]
    private static extern int[] getPurchasedIds ();
    [DllImport ("__Internal")]
    private static extern void clearPurchasedIds ();
    [DllImport ("__Internal")]
    private static extern void clearPurchasedId (int billingId);
    [DllImport ("__Internal")]
    private static extern bool isAdsEnabled ();
    [DllImport ("__Internal")]
    private static extern void setAdsEnable (bool enable);
    [DllImport ("__Internal")]
    private static extern void restorePayments ();
    [DllImport ("__Internal")]
    private static extern string getPaymentDatas ();
    [DllImport ("__Internal")]
    private static extern string getExtraData ();
    [DllImport ("__Internal")]
    private static extern string getConfig (int configId);
    [DllImport ("__Internal")]
    private static extern void login ();
    [DllImport ("__Internal")]
    private static extern void logout ();
    [DllImport ("__Internal")]
    private static extern bool isLogin ();
    [DllImport ("__Internal")]
    private static extern string meFirstName ();
    [DllImport ("__Internal")]
    private static extern string meLastName ();
    [DllImport ("__Internal")]
    private static extern string meId ();
    [DllImport ("__Internal")]
    private static extern string meName ();
    [DllImport ("__Internal")]
    private static extern string me ();
    [DllImport ("__Internal")]
    private static extern string friends ();
    [DllImport ("__Internal")]
    private static extern string mePictureURL ();
    [DllImport ("__Internal")]
    private static extern void fetchFriends (bool invitable);
    [DllImport ("__Internal")]
    private static extern void fetchScores ();
    [DllImport ("__Internal")]
    private static extern void invite ();
    [DllImport ("__Internal")]
    private static extern void share ();
    [DllImport ("__Internal")]
    private static extern void shareContent (string contentURL, string tag, string quote);
    [DllImport ("__Internal")]
    private static extern void logPlayerLevel (int levelId);
    [DllImport ("__Internal")]
    private static extern void logPageStart (string pageName);
    [DllImport ("__Internal")]
    private static extern void logPageEnd (string pageName);
    [DllImport ("__Internal")]
    private static extern void logEvent (string eventId);
    [DllImport ("__Internal")]
    private static extern void logEventWithTag (string eventId, string tag);
    [DllImport ("__Internal")]
    private static extern void logEventWithData (string eventId, string data);
    [DllImport ("__Internal")]
    private static extern void logStartLevel (string level);
    [DllImport ("__Internal")]
    private static extern void logFailLevel (string level);
    [DllImport ("__Internal")]
    private static extern void logFinishLevel (string level);
    [DllImport ("__Internal")]
    private static extern void logPay (double money, string itemName, int count, double price);
    [DllImport ("__Internal")]
    private static extern void logBuy (string itemName, int count, double price);
    [DllImport ("__Internal")]
    private static extern void logUse (string itemName, int count, double price);
    [DllImport ("__Internal")]
    private static extern void logBonus (string itemName, int count, double price, int trigger);
    [DllImport ("__Internal")]
    private static extern void sdklog (string info);
    [DllImport ("__Internal")]
    private static extern void toast (string info);
    private static RiseSdk _instance = null;
    private static bool hasInit = false;


    public static RiseSdk Instance {
        get {
            if (null == _instance)
                _instance = new RiseSdk ();
            return _instance;
        }
    }

    public void Init () {
        RiseEditorAd.hasInit = true;
        if (hasInit) {
            return;
        }
        hasInit = true;
        RiseSdkListener.Instance.enabled = true;
#if UNITY_EDITOR
#else
        onCreate ();
#endif
        lruCache = new FileLRUCache (20);
    }

    public int GetScreenWidth () {
#if UNITY_EDITOR
        return Screen.width;
#else
        return getScreenWidth ();
#endif
    }

    public int GetScreenHeight () {
#if UNITY_EDITOR
        return Screen.height;
#else
        return getScreenHeight ();
#endif
    }

    public void ShowBanner (int pos) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowBanner (pos);
#else
        showBanner (pos);
#endif
    }

    public void ShowBanner (string tag, int pos) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowBanner (tag, pos);
#else
        showBannerWithTag (tag, pos);
#endif
    }

    public void CloseBanner () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.CloseBanner ();
#else
        closeBanner ();
#endif
    }

    public bool HasRewardAd () {
#if UNITY_EDITOR
        return true;
#else
        return isVideoAvaliable ();
#endif
    }

    public bool HasRewardAd (string tag) {
#if UNITY_EDITOR
        return true;
#else
        return isVideoAvaliableWithTag (tag);
#endif
    }

    public void ShowRewardAd (int placementId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowRewardAd (placementId);
#else
        showRewardVideo (placementId);
#endif
    }

    public void ShowRewardAd (string tag, int placementId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowRewardAd (tag, placementId);
#else
        showRewardVideoWithTag (tag, placementId);
#endif
    }

    public void ShowAd (string tag) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowAd (tag);
#else
        showInterstitialAd (tag);
#endif
    }

    public void LoadAd (string tag) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("LoadAd: " + tag);
#else
        loadInterstitialAd (tag);
#endif
    }

    public void ShowAd (string tag, int delayShowSeconds) {
#if UNITY_EDITOR
        RiseSdkListener.Instance.StartCoroutine (delayStart ((string t) => RiseEditorAd.EditorAdInstance.ShowAd (t), tag, delayShowSeconds));
#else
        showInterstitialAdWithTag (tag, delayShowSeconds);
#endif
    }

    public void ShowAd (string tag, int delayShowSeconds, double delayTimeInterval) {
#if UNITY_EDITOR
        RiseSdkListener.Instance.StartCoroutine (delayStart ((string t) => RiseEditorAd.EditorAdInstance.ShowAd (t), tag, (float) (delayShowSeconds * delayTimeInterval)));
#else
        showInterstitialAdWithTag2 (tag, delayShowSeconds, delayTimeInterval);
#endif
    }

    private IEnumerator delayStart (Action<string> action, string tag, float delaySeconds) {
        if (delaySeconds > 0) {
            yield return new WaitForSeconds (delaySeconds);
        }
        if (action != null) {
            action (tag);
        }
        yield return null;
    }

    public void ShowIconAd (float width, float xPercent, float yPercent) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.ShowIconAd (width, xPercent, yPercent);
#else
        showIconAd (width, xPercent, yPercent);
#endif
    }

    public void CloseIconAd () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.CloseIconAd ();
#else
        closeIconAd ();
#endif
    }

    public void Rate () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Rate");
#else
        rateInApp ();
#endif
    }

    public bool IsNetworkConnected () {
#if UNITY_EDITOR
        return true;
#else
        return isNetworkAvaliable ();
#endif
    }

    public void Pay (int billingId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Pay (billingId);
#else
        pay (billingId);
#endif
    }

    public void CheckSubscriptionActive (int billingId) {
#if UNITY_EDITOR
        RiseSdkListener.Instance.onCheckSubscriptionResult (billingId + "," + 0);
#else
        isSubscriptionActive (billingId);
#endif
    }

    public int[] GetPurchasedIds () {
#if UNITY_EDITOR
        return new int[] { 1, 2, 3 };
#else
        return getPurchasedIds ();
#endif
    }

    public void ClearPurchasedIds () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("ClearPurchasedIds");
#else
        clearPurchasedIds ();
#endif
    }

    public void ClearPurchasedId (int billingId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("ClearPurchasedId, " + billingId);
#else
        clearPurchasedId (billingId);
#endif
    }

    public bool IsAdsEnabled () {
#if UNITY_EDITOR
        return true;
#else
        return isAdsEnabled ();
#endif
    }

    public void SetAdsEnable (bool enable) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("SetAdsEnable: " + enable);
#else
        setAdsEnable (enable);
#endif
    }

    public void RestorePayments () {
#if UNITY_EDITOR
        RiseSdkListener.Instance.onRestoreSuccess ("1");
        RiseEditorAd.EditorAdInstance.Toast ("RestorePayments");
#else
        restorePayments ();
#endif
    }

    public string GetPaymentDatas () {
#if UNITY_EDITOR
        return "";
#else
        return getPaymentDatas ();
#endif
    }

    public string GetExtraData () {
#if UNITY_EDITOR
        return "";
#else
        return getExtraData ();
#endif
    }

    public string GetConfig (int configId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("GetConfig, configId: " + configId);
        return "";
#else
        return getConfig (configId);
#endif
    }

    public void Login () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Login");
#else
        login();
#endif
    }

    public bool IsLogin () {
#if UNITY_EDITOR
        return false;
#else
        return isLogin ();
#endif
    }

    public void Logout () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Logout");
#else
        logout ();
#endif
    }

    public string GetMeFirstName () {
#if UNITY_EDITOR
        return "FirstName";
#else
        return meFirstName ();
#endif
    }

    public string GetMeLastName () {
#if UNITY_EDITOR
        return "LastName";
#else
        return meLastName ();
#endif
    }

    public string GetMeId () {
#if UNITY_EDITOR
        return "12345678";
#else
        return meId ();
#endif
    }

    public string GetMeName () {
#if UNITY_EDITOR
        return "12345678";
#else
        return meName ();
#endif
    }

    public string Me () {
#if UNITY_EDITOR
        return "{\"id\":\"12345678\", \"name\":\"hahaha\", \"picture\":\"http://img.qq1234.org/uploads/allimg/141205/3_141205195713_3.jpg\"}";
#else
        return me ();
#endif
    }

    public string GetFriends () {
#if UNITY_EDITOR
        return "[]";
#else
        return friends ();
#endif
    }

    public string GetMePictureURL () {
#if UNITY_EDITOR
        return "http://img.qq1234.org/uploads/allimg/141205/3_141205195713_3.jpg";
#else
        return mePictureURL ();
#endif
    }


    public void FetchFriends (bool invitable) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("FetchFriends");
#else
        fetchFriends (invitable);
#endif
    }

    public void FetchScores () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("FetchScores");
#else
        FetchScores ();
#endif
    }

    public void Invite () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Invite");
#else
        invite ();
#endif
    }

    public void Share (string contentURL, string tag, string quote) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Share: " + contentURL + ", " + tag + ", " + quote);
#else
        shareContent (contentURL, tag, quote);
#endif
    }

    public void Share () {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Share");
#else
        share ();
#endif
    }

    public void Toast (string message) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("toast: " + message);
#else
        toast (message);
#endif
    }

    public void SdkLog (string message) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("sdk log: " + message);
#else
        sdklog (message);
#endif
    }

    public void TrackEvent (string category, string keyValueData) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Track: " + category + "\n" + keyValueData);
#else
        logEventWithData (category, keyValueData);
#endif
    }

    public void UM_setPlayerLevel (int level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, setPlayerLevel: " + level);
#else
        logPlayerLevel (level);
#endif
    }

    public void UM_onEvent (String eventId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onEvent: " + eventId);
#else
        logEvent (eventId);
#endif
    }

    public void UM_onEvent (String eventId, String eventLabel) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onEvent: " + eventId + ", " + eventLabel);
#else
        logEventWithTag (eventId, eventLabel);
#endif
    }

    public void UM_onPageStart (String pageName) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onPageStart: " + pageName);
#else
        logPageStart (pageName);
#endif
    }

    public void UM_onPageEnd (String pageName) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, onPageEnd: " + pageName);
#else
        logPageEnd (pageName);
#endif
    }

    public void UM_startLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, startLevel: " + level);
#else
        logStartLevel (level);
#endif
    }

    public void UM_failLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, failLevel: " + level);
#else
        logFailLevel (level);
#endif
    }

    public void UM_finishLevel (String level) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, finishLevel: " + level);
#else
        logFinishLevel (level);
#endif
    }

    public void UM_pay (double money, String itemName, int number, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, pay, money: " + money + ", item: " + itemName + ", number: " + number + ", price: " + price);
#else
	    logPay (money, itemName, number, price);
#endif
    }

    public void UM_buy (String itemName, int count, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, buy, item: " + itemName + ", count: " + count + ", price: " + price);
#else
        logBuy (itemName, count, price);
#endif
    }

    public void UM_use (String itemName, int number, double price) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, use, item: " + itemName + ", number: " + number + ", price: " + price);
#else
        logUse (itemName, number, price);
#endif
    }

    public void UM_bonus (String itemName, int number, double price, int trigger) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("Umeng, bonus, item: " + itemName + ", number: " + number + ", price: " + price + ", trigger: " + trigger);
#else
        logBonus (itemName, number, price, trigger);
#endif
    }

    #region IOS_DUMMY
    public void UM_onEventValue (string eventId, Dictionary<string, string> mapStr) {
    }

    public void OnResume () {
    }

    public void OnPause () {
    }

    public void OnStart () {
    }

    public void OnStop () {
    }

    public void OnDestroy () {
    }

    public void OnExit () {
    }

    public string GetConfig (string packageName, int configId) {
#if UNITY_EDITOR
        RiseEditorAd.EditorAdInstance.Toast ("GetConfig, packageName: " + packageName + ", configId: " + configId);
#endif
        return "";
    }
    #endregion

#endif

    private FileLRUCache lruCache = null;
    public void DownloadFile (string url, Action<string, WWW> resultEvent) {
        lruCache.DownloadFile (url, resultEvent);
    }

    public void LoadLocalFile (string filePath, Action<string, WWW> resultEvent) {
        lruCache.LoadLocalFile (filePath, resultEvent);
    }


    //其他广告类型
    public const int ADTYPE_OTHER = -1;
    //大屏广告类型
    public const int ADTYPE_INTERTITIAL = 1;
    //横幅广告类型
    public const int ADTYPE_BANNER = 2;
    //视频广告类型
    public const int ADTYPE_VIDEO = 3;
    //icon广告类型
    public const int ADTYPE_ICON = 4;

    /// <summary>
    /// 在左上角显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_LEFT_TOP = 1;
    /// <summary>
    /// 在顶部居中显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_MIDDLE_TOP = 3;
    /// <summary>
    /// 在右上角显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_RIGHT_TOP = 6;
    /// <summary>
    /// 在中间居中显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_MIDDLE_MIDDLE = 5;
    /// <summary>
    /// 在左下角显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_LEFT_BOTTOM = 2;
    /// <summary>
    /// 在底部居中显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_MIDDLE_BOTTOM = 4;
    /// <summary>
    /// 在右下角显示banner广告参数常量
    /// </summary>
    public const int POS_BANNER_RIGHT_BOTTOM = 7;
    public const int POS_BANNER_LEFT_MIDDLE = 8;
    public const int POS_BANNER_RIGHT_MIDDLE = 9;

    /// <summary>
    /// 游戏打开时显示大屏广告参数常量
    /// </summary>
    public const string M_START = "start";
    /// <summary>
    /// 暂停游戏时显示大屏广告参数常量
    /// </summary>
    public const string M_PAUSE = "pause";
    /// <summary>
    /// 游戏过关时显示大屏广告参数常量
    /// </summary>
    public const string M_PASSLEVEL = "passlevel";
    /// <summary>
    /// 自定义时机显示大屏广告参数常量
    /// </summary>
    public const string M_CUSTOM = "custom";

    /// <summary>
    /// 计费成功标志常量
    /// </summary>
    public const int PAYMENT_RESULT_SUCCESS = 1;
    /// <summary>
    /// 计费失败标志常量
    /// </summary>
    public const int PAYMENT_RESULT_FAILS = 2;
    /// <summary>
    /// 计费取消标志常量
    /// </summary>
    public const int PAYMENT_RESULT_CANCEL = 3;

    /// <summary>
    /// 获取配置的AppId参数常量
    /// </summary>
    public const int CONFIG_KEY_APP_ID = 1;
    /// <summary>
    /// 获取配置的排行榜URL参数常量
    /// </summary>
    public const int CONFIG_KEY_LEADER_BOARD_URL = 2;
    /// <summary>
    /// 获取API Version参数常量
    /// </summary>
    public const int CONFIG_KEY_API_VERSION = 3;
    /// <summary>
    /// 获取本机屏幕宽度参数常量
    /// </summary>
    public const int CONFIG_KEY_SCREEN_WIDTH = 4;
    /// <summary>
    /// 获取本机屏幕高度参数常量
    /// </summary>
    public const int CONFIG_KEY_SCREEN_HEIGHT = 5;
    /// <summary>
    /// 获取本机语言参数常量
    /// </summary>
    public const int CONFIG_KEY_LANGUAGE = 6;
    /// <summary>
    /// 获取本机国家码参数常量
    /// </summary>
    public const int CONFIG_KEY_COUNTRY = 7;
    /// <summary>
    /// 获取应用的版本号参数常量
    /// </summary>
    public const int CONFIG_KEY_VERSION_CODE = 8;
    /// <summary>
    /// 获取应用的版本号名称参数常量
    /// </summary>
    public const int CONFIG_KEY_VERSION_NAME = 9;
    /// <summary>
    /// 获取应用的包名参数常量
    /// </summary>
    public const int CONFIG_KEY_PACKAGE_NAME = 10;

    /// <summary>
    /// 广告事件类型
    /// </summary>
    public enum AdEventType : int {
        FullAdLoadCompleted = 1,
        FullAdLoadFailed,
        RewardAdLoadFailed,
        RewardAdLoadCompleted,
        RewardAdShowStart,
        RewardAdShowFinished,
        RewardAdShowFailed,
        RewardAdClosed,
        /// <summary>
        /// 大屏广告被关闭
        /// </summary>
        FullAdClosed,
        FullAdShown,
        /// <summary>
        /// 大屏广告被点击
        /// </summary>
        FullAdClicked,
        /// <summary>
        /// bannner广告被点击
        /// </summary>
        BannerAdClicked,
        /// <summary>
        /// 交叉推广广告被点击
        /// </summary>
        CrossAdClicked,
        AdLoadCompleted,
        AdLoadFailed,
        AdShown,
        AdClosed
    }

    public enum PaymentResult : int {
        Success = 1,
        Failed,
        Cancel,
        PaymentSystemError
    }

    /// <summary>
    /// faceboook事件回调类型
    /// </summary>
    public enum SnsEventType : int {
        /// <summary>
        /// faceboook登陆成功
        /// </summary>
        LoginSuccess = 1,
        LoginFailed,
        /// <summary>
        /// faceboook邀请好友成功
        /// </summary>
        InviteSuccess,
        InviteFailed,
        /// <summary>
        /// faceboook挑战好友成功
        /// </summary>
        ChallengeSuccess,
        ChallengeFailed,
        /// <summary>
        /// faceboook给好友点赞成功
        /// </summary>
        LikeSuccess,
        LikeFailed,
        ShareSuccess,
        ShareFailed,
        ShareCancel
    }

    public static double GetCurrentTimeInMills () {
        TimeSpan span = DateTime.Now.Subtract (new DateTime (1970, 1, 1, 0, 0, 0));
        return span.TotalMilliseconds;
    }

    public static string CalculateMD5Hash (string input) {
        StringBuilder sb = new StringBuilder ();
        try {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider ();
            byte[] inputBytes = UTF8Encoding.Default.GetBytes (input);
            inputBytes = md5.ComputeHash (inputBytes);
            for (int i = 0; i < inputBytes.Length; i++) {
                sb.Append (inputBytes[i].ToString ("X2"));
            }
            return sb.ToString ();
        } catch (System.Exception ex) {
            Debug.LogError ("CalculateMD5Hash error:\n" + ex.StackTrace);
        } finally {
        }
        return sb.ToString ();
    }


    /// <summary>
    /// Editor模式下的广告测试类，不可以调用该类的方法。
    /// </summary>
    private class RiseEditorAd : MonoBehaviour {

        private static RiseEditorAd _editorAdInstance = null;
        public static bool hasInit = false;
        private Rect bannerPos;
        private bool bannerShow = false;
        private string bannerContent = "";
        private bool interstitialShow = false;
        private string interstitialContent = "";
        private bool rewardShow = false;
        private string rewardContent = "";
        private float scaleWidth = 1;
        private float scaleHeight = 1;
        private int originScreenWidth = 1;
        private int originScreenHeight = 1;
        private bool toastShow = false;
        private List<string> toastList = new List<string> ();
        private GUIStyle toastStyle = null;
        private int rewardAdId = NONE_REWARD_ID;
        private string rewardAdTag = DEFAULT_REWARD_TAG;
        private float iconAdWidth = 56;
        private float iconAdXPercent = .2f;
        private float iconAdYPercent = .2f;
        private bool iconAdShow = false;
        private string iconAdContent = "Icon Ad";
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
        private EventSystem curEvent = null;
#endif

        private const int NONE_REWARD_ID = -10;
        private const string DEFAULT_REWARD_TAG = "DEFAULT";
        private const string BANNER_DEFAULT_TXT = "Banner AD: ";
        private const string INTERSTITIAL_DEFAULT_TXT = "\nInterstitial AD Test";
        private const string REWARD_DEFAULT_TXT = "Free Coin AD Test: ";
        private const int SCREEN_WIDTH = 854;
        private const int SCREEN_HEIGHT = 480;
        private const int GUI_DEPTH = -99;
        private const int BANNER_WIDTH = 320;
        private const int BANNER_HEIGHT = 50;

        void Awake () {
            if (_editorAdInstance == null) {
                _editorAdInstance = this;
            }
            DontDestroyOnLoad (gameObject);
            if (Screen.width > Screen.height) {
                originScreenWidth = SCREEN_WIDTH;
                originScreenHeight = SCREEN_HEIGHT;
            } else {
                originScreenWidth = SCREEN_HEIGHT;
                originScreenHeight = SCREEN_WIDTH;
            }
            scaleWidth = Screen.width * 1f / originScreenWidth;
            scaleHeight = Screen.height * 1f / originScreenHeight;
            toastStyle = new GUIStyle ();
            toastStyle.fontStyle = FontStyle.Bold;
            toastStyle.alignment = TextAnchor.MiddleCenter;
            toastStyle.fontSize = 30;
        }

        public static RiseEditorAd EditorAdInstance {
            get {
                if (_editorAdInstance == null) {
                    _editorAdInstance = FindObjectOfType<RiseEditorAd> () == null ? new GameObject ("RiseEditorAd").AddComponent<RiseEditorAd> () : _editorAdInstance;
                }
                if (!hasInit) {
                    Debug.LogError ("Fatal Error: \nNeed Call RiseSdk.Instance.Init () First At Initialize Scene");
                }
                return _editorAdInstance;
            }
        }

#if UNITY_EDITOR
        void OnGUI () {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
            if (curEvent == null) {
                curEvent = EventSystem.current;
            }
#endif
            GUI.depth = GUI_DEPTH;
            if (bannerShow) {
                GUI.backgroundColor = Color.green;
                GUI.color = Color.red;
                if (GUI.Button (bannerPos, bannerContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                GUI.backgroundColor = Color.green;
                if (GUI.Button (bannerPos, bannerContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                GUI.backgroundColor = Color.green;
                if (GUI.Button (bannerPos, bannerContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                GUI.backgroundColor = Color.green;
                if (GUI.Button (bannerPos, bannerContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
            }
            if (interstitialShow) {
                GUI.backgroundColor = Color.black;
                GUI.color = Color.white;
                //GUI.backgroundColor = new Color (0, 0, 0, 1);
                //GUI.color = new Color (1, 0, 0, 1);
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    InterstitialAdCallBack ();
                    interstitialShow = false;
                    Instance.OnResume ();
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), interstitialContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), interstitialContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), interstitialContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), interstitialContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                GUI.backgroundColor = Color.red;
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    InterstitialAdCallBack ();
                    interstitialShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    InterstitialAdCallBack ();
                    interstitialShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    InterstitialAdCallBack ();
                    interstitialShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    InterstitialAdCallBack ();
                    interstitialShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
            }
            if (rewardShow) {
                GUI.backgroundColor = Color.black;
                GUI.color = Color.white;
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    rewardShow = false;
                    Instance.OnResume ();
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                    RewardAdCallBack ();
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), rewardContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), rewardContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), rewardContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                if (GUI.Button (new Rect (0, 0, Screen.width, Screen.height), rewardContent)) {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                }
                GUI.backgroundColor = Color.red;
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    rewardShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                    RewardAdCallBack ();
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    rewardShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                    RewardAdCallBack ();
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    rewardShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                    RewardAdCallBack ();
                }
                if (GUI.Button (new Rect (Screen.width - 100 * scaleWidth, 0, 100 * scaleWidth, 50 * scaleHeight), "Close")) {
                    rewardShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                    if (EventSystem.current != null) {
                        EventSystem.current.enabled = false;
                    }
#endif
                    RewardAdCallBack ();
                }
            }
            if (iconAdShow) {
                GUI.backgroundColor = Color.black;
                GUI.color = Color.white;
                //GUI.backgroundColor = new Color (0, 0, 0, 1);
                //GUI.color = new Color (1, 0, 0, 1);
                if (GUI.Button (new Rect (Screen.width * iconAdXPercent, Screen.height * iconAdYPercent, iconAdWidth, iconAdWidth), iconAdContent)) {
                }
                if (GUI.Button (new Rect (Screen.width * iconAdXPercent, Screen.height * iconAdYPercent, iconAdWidth, iconAdWidth), iconAdContent)) {
                }
                if (GUI.Button (new Rect (Screen.width * iconAdXPercent, Screen.height * iconAdYPercent, iconAdWidth, iconAdWidth), iconAdContent)) {
                }
                if (GUI.Button (new Rect (Screen.width * iconAdXPercent, Screen.height * iconAdYPercent, iconAdWidth, iconAdWidth), iconAdContent)) {
                }
            }
            if (toastList.Count > 0) {
                GUI.backgroundColor = Color.black;
                GUI.color = Color.red;
                //GUI.contentColor = Color.red;
                GUI.Button (new Rect ((Screen.width - 400 * scaleWidth) * .5f, Screen.height - 100 * scaleHeight, 400 * scaleWidth, 50 * scaleHeight), toastList[0]);
                GUI.Button (new Rect ((Screen.width - 400 * scaleWidth) * .5f, Screen.height - 100 * scaleHeight, 400 * scaleWidth, 50 * scaleHeight), toastList[0]);
                //GUI.Label (new Rect ((Screen.width - 200 * scaleWidth) * .5f, Screen.height - 100 * scaleHeight, 200 * scaleWidth, 50 * scaleHeight), toastList [0], toastStyle);
            }
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
            if (EventSystem.current != null) {
                EventSystem.current.enabled = true;
            } else if (curEvent != null) {
                curEvent.enabled = true;
                EventSystem.current = curEvent;
            }
#endif
        }

        void Update () {
            if (Input.GetKeyDown (KeyCode.Escape)) {
                interstitialShow = false;
                rewardShow = false;
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
                if (EventSystem.current != null) {
                    EventSystem.current.enabled = true;
                } else if (curEvent != null) {
                    curEvent.enabled = true;
                    EventSystem.current = curEvent;
                }
#endif
            }
        }

        private void InterstitialAdCallBack () {
            if (interstitialShow) {
#if UNITY_IOS
				RiseSdkListener.Instance.adDidClose ("custom");
#endif
            }
        }

        private void RewardAdCallBack () {
            if (rewardAdId != NONE_REWARD_ID) {
                Toast ("Show Reward Ad Success");
#if UNITY_ANDROID
                RiseSdkListener.Instance.onReceiveReward ("0|" + rewardAdId);
#elif UNITY_IOS
				RiseSdkListener.Instance.adReward (rewardAdTag + "|" + rewardAdId);
#endif
            }
            rewardAdId = NONE_REWARD_ID;
            rewardAdTag = DEFAULT_REWARD_TAG;
        }
#endif

        public void ShowBanner (int pos) {
#if UNITY_EDITOR
            bannerContent = BANNER_DEFAULT_TXT + "default";
            bannerShow = true;
            SetBannerPos (pos);
#endif
        }

        public void ShowBanner (string tag, int pos) {
#if UNITY_EDITOR
            bannerContent = BANNER_DEFAULT_TXT + tag;
            bannerShow = true;
            SetBannerPos (pos);
#endif
        }

        public void CloseBanner () {
#if UNITY_EDITOR
            bannerShow = false;
#endif
        }

        private void SetBannerPos (int pos) {
#if UNITY_EDITOR
            switch (pos) {
                case RiseSdk.POS_BANNER_LEFT_BOTTOM:
                    bannerPos = new Rect (0, Screen.height - BANNER_HEIGHT * scaleHeight, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_LEFT_TOP:
                    bannerPos = new Rect (0, 0, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_MIDDLE_BOTTOM:
                    bannerPos = new Rect ((Screen.width - BANNER_WIDTH * scaleWidth) * .5f, Screen.height - BANNER_HEIGHT * scaleHeight, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_MIDDLE_MIDDLE:
                    bannerPos = new Rect ((Screen.width - BANNER_WIDTH * scaleWidth) * .5f, (Screen.height - BANNER_HEIGHT * scaleHeight) * .5f, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_MIDDLE_TOP:
                    bannerPos = new Rect ((Screen.width - BANNER_WIDTH * scaleWidth) * .5f, 0, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_RIGHT_BOTTOM:
                    bannerPos = new Rect (Screen.width - BANNER_WIDTH * scaleWidth, Screen.height - BANNER_HEIGHT * scaleHeight, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
                case RiseSdk.POS_BANNER_RIGHT_TOP:
                    bannerPos = new Rect (Screen.width - BANNER_WIDTH * scaleWidth, 0, BANNER_WIDTH * scaleWidth, BANNER_HEIGHT * scaleHeight);
                    break;
            }
#endif
        }

        public void ShowAd (string tag) {
#if UNITY_EDITOR
            interstitialShow = true;
            interstitialContent = tag + INTERSTITIAL_DEFAULT_TXT;
            Instance.OnPause ();
#endif
        }

        public void ShowRewardAd (int id) {
#if UNITY_EDITOR
            rewardShow = true;
            rewardAdId = id;
            rewardAdTag = DEFAULT_REWARD_TAG;
            rewardContent = REWARD_DEFAULT_TXT + rewardAdTag;
            Instance.OnPause ();
#endif
        }

        public void ShowRewardAd (string tag, int id) {
#if UNITY_EDITOR
            rewardShow = true;
            rewardAdId = id;
            rewardAdTag = tag;
            rewardContent = REWARD_DEFAULT_TXT + tag;
            Instance.OnPause ();
#endif
        }

        public void ShowIconAd (float width, float xPercent, float yPercent) {
            iconAdShow = true;
            iconAdWidth = width;
            iconAdXPercent = xPercent;
            iconAdYPercent = yPercent;
        }

        public void CloseIconAd () {
            iconAdShow = false;
        }

        public void Pay (int billingId) {
#if UNITY_EDITOR
#if UNITY_ANDROID
            switch (EditorUtility.DisplayDialogComplex ("Pay", "Pay: " + billingId, "TRY FAILURE", "NO", "YES")) {
                case 0://TRY FAILURE
                    Toast ("pay " + billingId + " Failed");
                    RiseSdkListener.Instance.onPaymentFail (billingId + "");
                    break;
                case 1://NO
                    Toast ("pay " + billingId + " Canceled");
                    RiseSdkListener.Instance.onPaymentCanceled (billingId + "");
                    break;
                case 2://YES
                    Toast ("pay " + billingId + " Success");
                    RiseSdkListener.Instance.onPaymentSuccess (billingId + "");
                    break;
            }
#elif UNITY_IOS
            switch (EditorUtility.DisplayDialogComplex ("Pay", "Pay: " + billingId, "TRY FAILURE", "NO", "YES")) {
                case 0://TRY FAILURE
                    Toast ("pay " + billingId + " Failed");
                    RiseSdkListener.Instance.onPaymentFailure (billingId + "");
                    break;
                case 1://NO
                    Toast ("pay " + billingId + " Canceled");
					RiseSdkListener.Instance.onPaymentFailure (billingId + "");
                    break;
                case 2://YES
                    Toast ("pay " + billingId + " Success");
					RiseSdkListener.Instance.onPaymentSuccess (billingId + "");
                    break;
            }
#endif
#endif
        }

        private bool timeCounting = false;

        public void Toast (string msg) {
#if UNITY_EDITOR
            toastList.Add (msg);
            if (!timeCounting) {
                timeCounting = true;
                StartCoroutine (CheckToast ());
            }
#endif
        }

        private IEnumerator CheckToast (float time = 2) {
            yield return new WaitForSeconds (time);
            if (toastList.Count > 0) {
                toastList.RemoveAt (0);
            }
            if (toastList.Count > 0) {
                StartCoroutine (CheckToast ());
            } else {
                timeCounting = false;
            }
        }

        public void Alert (string title, string msg) {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog (title, msg, "NO", "OK");
#endif
        }

        public void OnExit () {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog ("Exit", "Are you sure to exit?", "YES", "NO")) {
                EditorApplication.isPlaying = false;
            }
#endif
        }

    }


    private class FileLRUCache {

        private int maxCapacity = 10;
        private int size = 0;
        private LinkedNode head = null;
        private LinkedNode tail = null;
        private Dictionary<string, LinkedNode> cache = null;

        private const string SD_ROOT_PATH = "/storage/emulated/";
        private const string SD_PATH = "/storage/emulated/0/.android/.filecache/";
        private const string CACHE_FILE = "filedirmeta";
        private const string SPLIT_FLAG = "@^@";
        private const string KEY_VALUE_SPLIT_FLAG = "^_^";
        private static string defFilePath = "/";

        public void DownloadFile (string url, Action<string, WWW> resultEvent) {
            if (string.IsNullOrEmpty (url)) {
                if (resultEvent != null) {
                    resultEvent ("", null);
                }
                return;
            }
            string saveName = CalculateMD5Hash (url);
            if (!File.Exists (defFilePath + saveName)) {
                RiseSdkListener.Instance.StartCoroutine (Download (url, saveName, resultEvent));
            } else {
                RiseSdkListener.Instance.StartCoroutine (LoadLocal (null, saveName, resultEvent));
            }
        }

        public void LoadLocalFile (string filePath, Action<string, WWW> resultEvent) {
            if (string.IsNullOrEmpty (filePath)) {
                if (resultEvent != null) {
                    resultEvent (filePath, null);
                }
            } else {
                if (File.Exists (filePath)) {
                    RiseSdkListener.Instance.StartCoroutine (LoadLocal (filePath, null, resultEvent));
                } else {
                    if (resultEvent != null) {
                        resultEvent (filePath, null);
                    }
                }
            }
        }

        public bool FileDownloaded (string url) {
            string saveName = CalculateMD5Hash (url);
            return File.Exists (defFilePath + saveName);
        }

        IEnumerator Download (string url, string saveName, Action<string, WWW> resultEvent) {
#if USE_WEB_REQUEST
        UnityWebRequest www = UnityWebRequest.Get (url);
        yield return www.Send ();
        Debug.LogWarning ("download isdone? " + www.isDone + ", data.Length: " + www.downloadHandler.data.Length + ", url: " + url);
        if (string.IsNullOrEmpty (www.error)) {
            if (www.downloadHandler != null && www.downloadHandler.data != null && www.downloadHandler.data.Length > 100) {
                byte [] bytesData = www.downloadHandler.data;
#else
            WWW www = new WWW (url);
            yield return www;
            set (saveName, defFilePath + saveName);
            if (string.IsNullOrEmpty (www.error)) {
                if (www.bytes != null && www.bytes.Length > 20) {
                    byte[] bytesData = www.bytes;
#endif
                    File.WriteAllBytes (defFilePath + saveName, bytesData);
                    if (resultEvent != null) {
                        resultEvent (defFilePath + saveName, www);
                    }
                } else if (resultEvent != null) {
                    resultEvent (defFilePath + saveName, null);
                }
            } else if (resultEvent != null) {
                resultEvent (defFilePath + saveName, null);
                Debug.LogError ("Download File error, url: " + url + ", saveName: " + saveName + ", www.error: " + www.error);
            }
        }

        IEnumerator LoadLocal (string filePath, string saveName, Action<string, WWW> resultEvent) {
            if (string.IsNullOrEmpty (filePath)) {
                filePath = defFilePath;
            }
            if (saveName == null) {
                saveName = "";
            }
            string path = "file:///" + filePath + saveName;
            WWW www = new WWW (path);
            yield return www;
            set (saveName, filePath + saveName);
            if (string.IsNullOrEmpty (www.error)) {
                if (www.bytes != null && www.bytes.Length > 20) {
                    if (resultEvent != null) {
                        resultEvent (filePath + saveName, www);
                    }
                } else if (resultEvent != null) {
                    resultEvent (filePath + saveName, null);
                }
            } else if (resultEvent != null) {
                resultEvent (filePath + saveName, null);
                Debug.LogError ("LoadLocal File error, filePath: " + filePath + ", saveName: " + saveName + ", www.error: " + www.error);
            }
        }

        public enum FileType {
            Image,
            Text
        }

        public FileLRUCache (int capacity) {
            maxCapacity = capacity;
            cache = new Dictionary<string, LinkedNode> ();
            head = new LinkedNode ();
            tail = new LinkedNode ();
            head.prev = null;
            head.next = tail;
            tail.prev = head;
            tail.next = null;
            try {
                defFilePath = Application.persistentDataPath + "/filecache/";
#if UNITY_ANDROID
                if (Directory.Exists (SD_ROOT_PATH)) {
                    defFilePath = SD_PATH;
                }
#endif
                if (!Directory.Exists (defFilePath)) {
                    Directory.CreateDirectory (defFilePath);
                }
            } catch (System.Exception ex) {
                Debug.LogError ("FileLRUCache init error\n" + ex.StackTrace);
                defFilePath = Application.persistentDataPath + "/filecache/";
            } finally {
                if (!Directory.Exists (defFilePath)) {
                    Directory.CreateDirectory (defFilePath);
                }
                string filePath = defFilePath + CACHE_FILE;
                if (!File.Exists (filePath)) {
                    File.Create (filePath);
                    RiseSdkListener.Instance.StartCoroutine (delayLoad (filePath, 1));
                } else {
                    LoadLocalFile (filePath, loadCache);
                }
            }
        }

        private IEnumerator delayLoad (string path, float delayTime) {
            yield return new WaitForSeconds (delayTime);
            LoadLocalFile (path, loadCache);
        }

        private void loadCache (string path, WWW www) {
            if (www != null) {
                string data = www.text;
                if (!string.IsNullOrEmpty (data)) {
                    string[] keyValues = data.Split (SPLIT_FLAG.ToCharArray ());
                    LinkedNode node = null;
                    LinkedNode tailPrev = null;
                    size = 0;
                    string[] keyValue = null;
                    for (int i = 0, len = keyValues.Length; i < len; i++) {
                        keyValue = null;
                        if (!string.IsNullOrEmpty (keyValues[i])) {
                            keyValue = keyValues[i].Split (KEY_VALUE_SPLIT_FLAG.ToCharArray ());
                        }
                        if (keyValue != null && keyValue.Length > 1 && !string.IsNullOrEmpty (keyValue[0]) && !string.IsNullOrEmpty (keyValue[1])) {
                            node = new LinkedNode ();
                            node.key = keyValue[0];
                            node.value = keyValue[1];
                            tailPrev = tail.prev;
                            tail.prev = node;
                            node.next = tail;
                            node.prev = tailPrev;
                            tailPrev.next = node;
                            cache.Add (keyValue[0], node);
                            size++;
                        }
                    }
                }
            }
        }

        private void writeCache () {
#if UNITY_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9
            if (writting != null) {
                RiseSdkListener.Instance.StopCoroutine (writting);
            }
#endif
            writting = RiseSdkListener.Instance.StartCoroutine (delayWrite ());
        }

        private Coroutine writting = null;
        private IEnumerator delayWrite () {
            yield return new WaitForSeconds (1);
            string str = "";
            LinkedNode node = head.next;
            while (node != null && node != tail) {
                str += node.key + KEY_VALUE_SPLIT_FLAG + node.value + SPLIT_FLAG;
                node = node.next;
            }
            str = str.Remove (str.Length - SPLIT_FLAG.Length, SPLIT_FLAG.Length);
            //FileStream fs = new FileStream (defFilePath + CACHE_FILE, FileMode.OpenOrCreate, FileAccess.Write);
            //byte [] strBytes = UTF8Encoding.UTF8.GetBytes (str);
            //fs.Write (strBytes, 0, strBytes.Length);
            //fs.Close ();
            //fs.Dispose ();
            File.WriteAllText (defFilePath + CACHE_FILE, str, UTF8Encoding.UTF8);
        }

        private void set (string key, string value) {
            if (string.IsNullOrEmpty (key) || string.IsNullOrEmpty (value)) {
                return;
            }
            LinkedNode node = null;
            if (cache.ContainsKey (key)) {
                node = cache[key];
                moveToFront (node);
            } else {
                node = new LinkedNode ();
                node.key = key;
                node.value = value;
                linkAtFront (node);
                cache.Add (key, node);
                size++;
            }
            checkCapacity ();
            writeCache ();
        }

        private void checkCapacity () {
            while (size > maxCapacity) {
                size--;
                removeLast ();
            }
        }

        private string get (string key) {
            if (!string.IsNullOrEmpty (key)) {
                return cache[key].value;
            }
            return "";
        }

        private void linkAtFront (LinkedNode node) {
            LinkedNode headNext = head.next;
            head.next = node;
            node.prev = head;
            node.next = headNext;
            headNext.prev = node;
        }

        private void moveToFront (LinkedNode node) {
            LinkedNode prevNode = node.prev;
            LinkedNode nextNode = node.next;
            if (prevNode == null || nextNode == null) {
                return;
            }
            prevNode.next = nextNode;
            nextNode.prev = prevNode;
            linkAtFront (node);
        }

        private void removeLast () {
            LinkedNode tailPrev = tail.prev;
            LinkedNode tailPrevPrev = tailPrev.prev;
            if (tailPrev == head || tailPrevPrev == null) {
                return;
            }
            tailPrevPrev.next = tail;
            tail.prev = tailPrevPrev;
            cache.Remove (tailPrev.key);
            File.Delete (tailPrev.value);
        }
    }


    private class LinkedNode {
        public string key = null;
        public string value = null;
        public LinkedNode prev = null;
        public LinkedNode next = null;
    }

}
