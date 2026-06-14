#!/usr/bin/env python3
import re

with open('/home/user/MifareOneTool/MifareOneTool/Form1.zh.resx', 'r', encoding='utf-8') as f:
    content = f.read()

# Translations: map from simplified to traditional
# We only replace inside <value>...</value> tags
translations = [
    ('扫描卡片', '掃描卡片'),
    ('锁UFUID卡', '鎖UFUID卡'),
    ('UID读', 'UID讀'),
    ('读取UID卡片。', '讀取UID卡片。'),
    ('UID写', 'UID寫'),
    ('写入UID卡片。', '寫入UID卡片。'),
    ('检加密', '檢加密'),
    ('检测卡片加密情况。', '檢測卡片加密情況。'),
    ('手动CLI', '手動CLI'),
    ('打开NFC命令行以进行高级操作。', '打開NFC命令列以進行高級操作。'),
    ('CUID写', 'CUID寫'),
    ('写入CUID/FUID卡片（可能需要密钥文件）', '寫入CUID/FUID卡片（可能需要密鑰文件）'),
    ('清终端', '清終端'),
    ('存日志', '存日誌'),
    ('字典测试', '字典測試'),
    ('导入字典文件进行Nested破解。', '匯入字典文件進行Nested破解。'),
    ('差异比较', '差異比較'),
    ('检测加密', '檢測加密'),
    ('知一密破解', '知一密破解'),
    ('写C/FUID卡', '寫C/FUID卡'),
    ('一键解原卡', '一鍵解原卡'),
    ('已知密钥读', '已知密鑰讀'),
    ('写入普通卡', '寫入普通卡'),
    ('从UID卡读回', '從UID卡讀回'),
    ('检测连接', '檢測連接'),
    ('加载密钥…', '載入密鑰…'),
    ('停止', '停止'),
    ('写(UF)UID卡', '寫(UF)UID卡'),
    ('Hex编辑器', 'Hex編輯器'),
    ('停运行', '停運行'),
    ('检测设备', '檢測設備'),
    ('扫描已连接的NFC设备\\n(目前支持PN532、ACR122U)', '掃描已連接的NFC設備\\n(目前支援PN532、ACR122U)'),
    ('锁Ufuid', '鎖Ufuid'),
    ('锁死UFUID卡片0块数据（测试中）', '鎖死UFUID卡片0塊數據（測試中）'),
    ('全加密爆破', '全加密爆破'),
    ('对卡片执行Darkside工具（不一定成功）', '對卡片執行Darkside工具（不一定成功）'),
    ('清M1', '清M1'),
    ('格式化普通M1卡（必须加载密钥文件）', '格式化普通M1卡（必須載入密鑰文件）'),
    ('对半加密卡片进行Nested破解。\\n按住Ctrl点击该按钮可添加已知密钥。', '對半加密卡片進行Nested破解。\\n按住Ctrl點擊該按鈕可添加已知密鑰。'),
    ('读M1', '讀M1'),
    ('读取普通M1卡片（可能需要加载密钥文件）', '讀取普通M1卡片（可能需要載入密鑰文件）'),
    ('写M1', '寫M1'),
    ('写入普通M1卡（可能需要加载密钥文件）', '寫入普通M1卡（可能需要載入密鑰文件）'),
    ('知n密', '知n密'),
    ('输入已知密钥进行Nested破解。', '輸入已知密鑰進行Nested破解。'),
    ('手动扫描', '手動掃描'),
    ('扫描有效卡片。', '掃描有效卡片。'),
    ('选择key.mfd', '選擇key.mfd'),
    ('加载含有正确读写卡密钥及正确控制位的MFD文件。', '載入含有正確讀寫卡密鑰及正確控制位的MFD文件。'),
    ('UID全格', 'UID全格'),
    ('将全卡清空并重新初始化。\\n可用于ACbit损坏/KEY全部被改等情况的急救。', '將全卡清空並重新初始化。\\n可用於ACbit損壞/KEY全部被改等情況的急救。'),
    ('重置UID卡片0块，UID随机，厂商号为复旦。', '重置UID卡片0塊，UID隨機，廠商號為復旦。'),
    ('UID写号', 'UID寫號'),
    ('向UID卡片写入置顶卡号，厂商设置为复旦。', '向UID卡片寫入置頂卡號，廠商設置為復旦。'),
    ('自动判断Key(beta)', '自動判斷Key(beta)'),
    ('自动加载uid.Key文件', '自動載入uid.Key文件'),
    ('自动以UID名保存文件', '自動以UID名保存文件'),
    ('CUID写空卡补丁', 'CUID寫空卡補丁'),
    ('自动转到高级操作模式', '自動轉到高級操作模式'),
    ('单线程计算', '單線程計算'),
    ('多实例运行模式 会禁用多开检测 请自行指定设备', '多實例運行模式 會禁用多開檢測 請自行指定設備'),
    ('减少找设备延迟', '減少找設備延遲'),
    ('数据写入保护(建议)', '數據寫入保護(建議)'),
    ('标准', '標準'),
    ('俄语', '俄語'),
    ('设备控制', '設備控制'),
    ('破解工具', '破解工具'),
    ('界面设置', '界面設置'),
    ('偏好设置', '偏好設置'),
    ('优化设置', '優化設置'),
    ('语言和地区', '語言和地區'),
    ('从这里开始', '從這裡開始'),
    ('普通卡操作', '普通卡操作'),
    ('运行/终端', '運行/終端'),
    ('集成辅助工具', '集成輔助工具'),
    ('[2]读取原卡', '[2]讀取原卡'),
    ('卡操作相关', '卡操作相關'),
    ('[3]写入新卡', '[3]寫入新卡'),
    ('该卡种读取\\n同普通卡', '該卡種讀取\\n同普通卡'),
    ('终端文字大小', '終端文字大小'),
    ('指定设备', '指定設備'),
    ('选择界面语言', '選擇界面語言'),
    ('本工具仅支持SAK=08/18/28的\\n卡片复制。SAK28无一键解密。\\n若要复制S70卡片，请在高级界\\n面上取消勾选「数据写入保护」。',
     '本工具僅支持SAK=08/18/28的\\n卡片複製。SAK28無一鍵解密。\\n若要複製S70卡片，請在高級界\\n面上取消勾選「數據寫入保護」。'),
    ('尝试一下是否成功', '嘗試一下是否成功'),
    ('请放\\n原卡', '請放\\n原卡'),
    ('请放\\n新卡', '請放\\n新卡'),
    ('终端文字颜色', '終端文字顏色'),
    ('计时器', '計時器'),
    ('就绪', '就緒'),
    ('高级操作模式', '高級操作模式'),
    ('复制卡模式', '複製卡模式'),
    ('软件设置', '軟件設置'),
    ('检查更新', '檢查更新'),
]

def replace_in_value_tags(content, old, new):
    """Replace old with new only inside <value>...</value> tags."""
    # We need to be careful about multiline values
    # Strategy: find all <value>...</value> blocks and replace within them
    result = []
    i = 0
    while i < len(content):
        # Find next <value>
        start = content.find('<value>', i)
        if start == -1:
            result.append(content[i:])
            break
        result.append(content[i:start + 7])  # up to and including <value>
        # Find the closing </value>
        end = content.find('</value>', start + 7)
        if end == -1:
            result.append(content[start + 7:])
            break
        value_content = content[start + 7:end]
        value_content = value_content.replace(old, new)
        result.append(value_content)
        result.append('</value>')
        i = end + 8
    return ''.join(result)

# Apply all translations
for old, new in translations:
    content = replace_in_value_tags(content, old, new)

# Insert comboBox1.Items2 after comboBox1.Items1 block
items2_entry = '''  <data name="comboBox1.Items2" xml:space="preserve">
    <value>繁體中文</value>
  </data>
'''

# Find the end of comboBox1.Items1 block
items1_pattern = r'(<data name="comboBox1\.Items1"[^>]*>.*?</data>)'
match = re.search(items1_pattern, content, re.DOTALL)
if match:
    end_pos = match.end()
    content = content[:end_pos] + '\n' + items2_entry + content[end_pos:]
    print("Inserted comboBox1.Items2 after comboBox1.Items1")
else:
    print("WARNING: Could not find comboBox1.Items1 to insert after!")

with open('/home/user/MifareOneTool/MifareOneTool/Form1.zh-TW.resx', 'w', encoding='utf-8') as f:
    f.write(content)

print("Done! Form1.zh-TW.resx written.")
