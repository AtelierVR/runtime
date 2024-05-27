// #if UNITY_EDITOR
// using System;
// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.CCK.Mods;
// using Nox.CCK.Users;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.UIElements;

// namespace api.nox.user
// {
//     [InitializeOnLoad, CCKMod("api.nox.user", "0.0.1")]
//     public class UserMod : CCKDescriptor
//     {
//         public User User { get; private set; } = null;
//         public override void OnCCKLoad()
//         {
//             root = null;
//             User = null;
//             Run().Forget();
//         }

//         public async UniTask Run()
//         {
//             User = await FetchUserMe();
//             UpdateUser(User);
//         }


//         public override void OnCCKUnload()
//         {
//             if (root == null) return;
//             var login = root.Q<VisualElement>("login");
//             var loginButton = login.Q<Button>("connect");
//             loginButton.clicked -= OnClickLogin;
//             var current = root.Q<VisualElement>("current");
//             var logoutButton = current.Q<Button>("disconnect");
//             logoutButton.clicked -= OnClickLogout;
//             root = null;
//             User = null;
//         }

//         private async void OnClickLogin()
//         {
//             if (root == null) return;
//             var login = root.Q<VisualElement>("login");
//             var loginIdentifier = login.Q<TextField>("identifier");
//             var loginPassword = login.Q<TextField>("password");
//             var loginServer = login.Q<TextField>("server");
//             var loginform = login.Q<VisualElement>("form");
//             var loginError = login.Q<VisualElement>("error");
//             var loginErrorMessage = login.Q<Label>("error-message");
//             loginform.SetEnabled(false);
//             var result = await FetchLogin(loginServer.value, loginIdentifier.value, loginPassword.value);
//             loginform.SetEnabled(true);
//             if (result == null || result.error?.code > 0)
//             {
//                 loginErrorMessage.text = result?.error?.message ?? "An error occured.";
//                 loginError.style.display = DisplayStyle.Flex;
//                 UpdateUser(null);
//                 return;
//             }
//             else
//             {
//                 loginError.style.display = DisplayStyle.None;
//                 UpdateUser(result.data.user);
//             }
//         }

//         private async void OnClickLogout()
//         {
//             var current = root.Q<VisualElement>("current");
//             var logoutButton = current.Q<Button>("disconnect");
//             logoutButton.SetEnabled(false);
//             var result = await FetchLogout();
//             logoutButton.SetEnabled(true);
//             if (result == null || result.error?.code > 0)
//             {
//                 Debug.LogError(result?.error?.message ?? "An error occured.");
//                 return;
//             }
//             else
//             {
//                 UpdateUser(null);
//             }
//         }

//         public VisualElement root;

//         public void UpdateUser(User user)
//         {
//             User = user;
//             if (root == null) return;
//             var login = root.Q<VisualElement>("login");
//             var current = root.Q<VisualElement>("current");
//             if (User != null)
//             {
//                 current.Q<TextField>("username").value = User.username;
//                 current.Q<TextField>("display").value = User.display;
//                 current.Q<TextField>("id").value = User.id + "@" + User.server;
//                 var taglist = current.Q<ListView>("tags");
//                 taglist.makeItem = () =>
//                 {
//                     var label = new Label();
//                     label.style.marginLeft = 4;
//                     label.style.marginRight = 4;
//                     return label;
//                 };
//                 taglist.bindItem = (e, i) => (e as Label).text = User.tags[i];
//                 taglist.itemsSource = User.tags;
//                 taglist.allowAdd = false;
//                 taglist.allowRemove = false;
//                 current.style.display = DisplayStyle.Flex;
//                 login.style.display = DisplayStyle.None;
//             }
//             else
//             {
//                 login.style.display = DisplayStyle.Flex;
//                 current.style.display = DisplayStyle.None;
//             }
//         }

//         [CCKTab("api.nox.user", "User")]
//         public VisualElement UserTab(bool selected = false)
//         {
//             if (!selected) return null;
//             if (root == null)
//             {
//                 root = Resources.Load<VisualTreeAsset>("UserMod").CloneTree();
//                 var login = root.Q<VisualElement>("login");
//                 var loginButton = login.Q<Button>("connect");
//                 loginButton.clicked += OnClickLogin;
//                 var current = root.Q<VisualElement>("current");
//                 var logoutButton = current.Q<Button>("disconnect");
//                 logoutButton.clicked += OnClickLogout;
//                 root.Q<Label>("version").text = "v" + Version;
//             }
//             UpdateUser(User);
//             return root;
//         }

        
//     }
// }
// #endif