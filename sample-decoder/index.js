module.exports = function(context, req) {
  if (!req.body.data) {
    context.res = {
      'error': 'No data found'
    };
    context.done();
    return;
  }

  var data = Buffer.from(req.body.data, 'hex').toString();
  context.log(data);
  var split = data.split(':');
  context.res = {
    'temperature': parseFloat(split[0]),
    'humidity': parseFloat(split[1])
  };
  context.done();
};
