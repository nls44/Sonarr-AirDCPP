import React from 'react';
import Label, { LabelProps } from 'Components/Label';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import styles from './ProtocolLabel.module.css';

interface ProtocolLabelProps
  extends Omit<LabelProps, 'children' | 'className'> {
  protocol: DownloadProtocol;
}

function getProtocolName(protocol: DownloadProtocol) {
  if (protocol === 'usenet') {
    return 'nzb';
  }

  if (protocol === 'directConnect') {
    return 'DC';
  }

  return protocol;
}

function ProtocolLabel({ protocol, ...otherProps }: ProtocolLabelProps) {
  const protocolName = getProtocolName(protocol);

  return (
    <Label className={styles[protocol]} {...otherProps}>
      {protocolName}
    </Label>
  );
}

export default ProtocolLabel;
