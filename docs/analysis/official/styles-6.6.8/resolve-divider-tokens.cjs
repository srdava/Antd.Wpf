const fs = require('fs');
const theme = require('antd/lib/theme').default;
const divider = require('antd/lib/divider/style').prepareComponentToken;
const result = { version: require('antd/package.json').version, method: 'Official getDesignToken + Divider prepareComponentToken', modes: {} };
for (const mode of ['Light', 'Dark']) {
 const token = theme.getDesignToken({ algorithm: mode === 'Light' ? theme.defaultAlgorithm : theme.darkAlgorithm });
 result.modes[mode] = { colorSplit: token.colorSplit, colorTextHeading: token.colorTextHeading, fontSize: token.fontSize, fontSizeLG: token.fontSizeLG, lineHeight: token.lineHeight, marginXS: token.marginXS, margin: token.margin, marginLG: token.marginLG, lineWidth: token.lineWidth, component: divider(token) };
}
fs.writeFileSync(process.argv[2], JSON.stringify(result, null, 2));
console.log(JSON.stringify(result));