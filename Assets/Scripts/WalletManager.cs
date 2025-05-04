using TMPro;
using UnityEngine;
using System;
using System.Numerics;
using Thirdweb;
using Thirdweb.Unity;

public class WalletManager : MonoBehaviour
{
    string address = "";
    const string tokenAddress = "0xFD2b81a411440a678855f484bb0b8d5C46c426E3";

    const int units = 18;

    IThirdwebWallet wallet;

    public TMP_Text Display;

    public async void ConnectWallet()
    {
        // 設定連接選項，這裡以 WalletConnect 為例
        var options = new WalletOptions(
            provider: WalletProvider.WalletConnectWallet,
            chainId: 11155111 // Sepolia 測試網的 Chain ID
        );

        // 連接錢包
        wallet = await ThirdwebManager.Instance.ConnectWallet(options);
        Display.text = "Connect succesfully";
        
    }

    // 取得錢包地址
    public async void GetAddress()
    {
        if(wallet == null)
        {
            Display.text = "Wallet is NULL";
            return;
        }

        address = await wallet.GetAddress();
        Display.text = "Wallet Address: " + address;
    }

    public async void GetBalance()
    {
        if (wallet == null)
        {
            Display.text = "Wallet is NULL";
            return;
        }

        if (address == "")
        {
            Display.text = "Address is NULL";
            return;
        }

        try
        {
            // 從 Resources 載入 ABI JSON 檔案
            TextAsset abiAsset = Resources.Load<TextAsset>("ABI/tnt1_abi");
            if (abiAsset == null)
            {
                throw new Exception("無法載入 ABI 檔案，請確認 Assets/Resources/ABI/GameTokenABI.json 是否存在");
            }

            // 將 TextAsset 轉為字串
            string abiJson = abiAsset.text;
            

            // 使用載入的 ABI 獲取合約
            var contract = await ThirdwebManager.Instance.GetContract(tokenAddress, 11155111, abiJson);

            // 讀取餘額
            BigInteger raw = await contract.Read<BigInteger>("balanceOf", address);

            decimal divisor = (decimal)Math.Pow(10, units);
            decimal balance = (decimal)raw / divisor;

            Display.text = $"Balance: {balance}";
        }
        catch (Exception ex)
        {
            Debug.LogError($"獲取餘額失敗: {ex.Message}\n{ex.StackTrace}");
            Display.text = $"Error: {ex.Message}";
        }
    }

    public async void MintTokens()
    {
        if (address == "")
        {
            Display.text = "Address is NULL";
            return;
        }

        // 目標數量（以代幣為單位，例如 100 個代幣）
        string amountStr = "100"; // 鑄造 100 個代幣

        try
        {
            // 從 Resources 載入 ABI JSON 檔案
            TextAsset abiAsset = Resources.Load<TextAsset>("ABI/tnt1_abi");
            if (abiAsset == null)
            {
                throw new Exception("無法載入 ABI 檔案，請確認 Assets/Resources/ABI/GameTokenABI.json 是否存在");
            }

            string abiJson = abiAsset.text;
            Debug.Log($"載入的 ABI: {abiJson}");

            // 使用載入的 ABI 獲取合約
            var contract = await ThirdwebManager.Instance.GetContract(tokenAddress, 11155111, abiJson);

            // 將 amountStr 轉換為 BigInteger，考慮小數位
            BigInteger amount = BigInteger.Parse(amountStr) * BigInteger.Pow(10, units); // 100 * 10^18

            // 調用 mint 函數
            var receipt = await contract.Write(
                wallet,                   // 簽名並發送的錢包
                "mint",                   // 合約函數名（改為 mint）
                BigInteger.Zero,          // 不發送額外原生幣
                address,                  // 第一個參數：接收者地址
                amount                    // 第二個參數：鑄造數量（BigInteger）
            );

            Debug.Log($"🛠️ 鑄造完成，交易哈希: {receipt}");
            Display.text = "Mint Successfully!";
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 鑄造失敗: {ex.Message}\n{ex.StackTrace}");
            Display.text = $"Mint failed: {ex.Message}";
        }
    }

    public async void TransferTokens()
    {
        if (wallet == null)
        {
            Display.text = "Wallet is NULL";
            return;
        }

        if (address == "")
        {
            Display.text = "Address is NULL";
            return;
        }

        string toAddress = "0xC0801ADA1Dc5EE235D154518DCcCd2e41793EbF8";
        string amountStr = "1000";

        if (string.IsNullOrEmpty(toAddress) || !toAddress.StartsWith("0x") || toAddress.Length != 42)
        {
            Display.text = "Invalid recipient address";
            return;
        }

        if (string.IsNullOrEmpty(amountStr) || !decimal.TryParse(amountStr, out decimal amountDecimal) || amountDecimal <= 0)
        {
            Display.text = "Invalid amount";
            return;
        }

        try
        {
            TextAsset abiAsset = Resources.Load<TextAsset>("ABI/tnt1_abi");
            if (abiAsset == null)
            {
                throw new Exception("無法載入 ABI 檔案，請確認 Assets/Resources/ABI/tnt1_abi.json 是否存在");
            }

            string abiJson = abiAsset.text;
            Debug.Log($"載入的 ABI: {abiJson}");

            var contract = await ThirdwebManager.Instance.GetContract(tokenAddress, 11155111, abiJson);

            BigInteger amount = (BigInteger)(amountDecimal * (decimal)Math.Pow(10, units));

            var receipt = await contract.Write(
                wallet,
                "transfer",
                BigInteger.Zero,
                toAddress,
                amount
            );

            Debug.Log($"🛠️ 轉移完成，交易哈希: {receipt}");
            Display.text = $"Transfer {amountDecimal} tokens to {toAddress} Successfully!";
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 轉移失敗: {ex.Message}\n{ex.StackTrace}");
            Display.text = $"Transfer failed: {ex.Message}";
        }
    }
}
