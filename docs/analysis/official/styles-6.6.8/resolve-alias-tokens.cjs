const fs = require('fs');
const theme = require('antd/lib/theme').default;
const names = ['fontSize','fontSizeSM','fontSizeLG','lineHeight','lineHeightSM','lineHeightLG','fontWeightStrong','controlHeight','controlHeightSM','controlHeightLG','borderRadius','borderRadiusSM','borderRadiusLG','controlInteractiveSize','padding','paddingXS','paddingSM','paddingXXS','paddingContentHorizontal','marginXS','colorText','colorTextSecondary','colorTextTertiary','colorTextQuaternary','colorTextDisabled','colorTextLightSolid','colorBgLayout','colorBgContainer','colorBgElevated','colorBorder','colorBorderDisabled','colorFill','colorFillSecondary','colorFillTertiary','colorFillQuaternary','colorBgContainerDisabled','colorPrimary','colorPrimaryHover','colorPrimaryActive','colorPrimaryBg','colorPrimaryBgHover','colorError','colorErrorHover','colorErrorActive','controlOutline','controlTmpOutline','controlOutlineWidth','boxShadowSecondary','opacityLoading','colorErrorBg','colorErrorBgHover','colorErrorOutline','colorWarning','colorWarningBg','colorFillAlter','colorBgTextHover','colorBgTextActive','colorBgSolid','colorBgSolidHover','colorBgSolidActive','controlItemBgActive','controlItemBgHover','colorPrimaryBorder','colorWarningOutline','colorErrorBgFilledHover','colorErrorBgActive','colorLink','colorLinkHover','colorLinkActive'];
const all = {};
const componentTokens = {};
for(const mode of ['Light','Dark']){
 const t = theme.getDesignToken({algorithm: mode === 'Light' ? theme.defaultAlgorithm : theme.darkAlgorithm});
 all[mode] = Object.fromEntries(names.map(n=>[n,t[n]]));
 componentTokens[mode] = { Button: { solidTextColor: require('antd/lib/button/style/token').prepareComponentToken(t).solidTextColor } };
}
fs.writeFileSync(process.argv[2],JSON.stringify({version:require('antd/package.json').version,method:'Official antd/lib/theme getDesignToken, default and dark algorithms; no component overrides',tokens:all,componentTokens},null,2));
console.log(JSON.stringify(all,null,2));
