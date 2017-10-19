var temperature = Math.random() * (25 - 20) + 20;
var data = temperature.toFixed(2) + ':36.21';
var timestamp = Date.now();

var testData = {
  'cmd': 'rx',
  'seqno': 1854,
  'EUI': 'BE7A00000000190F',
  'ts': timestamp,
  'fcnt': 27,
  'port': 1,
  'freq': 867100000,
  'rssi': -25,
  'snr': 10,
  'toa': 61,
  'dr': 'SF7',
  'ack': false,
  'bat': 255,
  'data': Buffer.from(data).toString('hex')
};

console.log(JSON.stringify(testData));
