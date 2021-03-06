﻿using System;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using MQTTnet.Core;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.TestApp.UniversalWindows
{
    public sealed partial class MainPage
    {
        private IMqttClient _mqttClient;

        public MainPage()
        {
            InitializeComponent();

            MqttTrace.TraceMessagePublished += OnTraceMessagePublished;
        }

        private async void OnTraceMessagePublished(object sender, MqttTraceMessagePublishedEventArgs e)
        {
            await Trace.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{e.Level}] [{e.Source}] [{e.ThreadId}] [{e.Message}]{Environment.NewLine}";
                if (e.Exception != null)
                {
                    text += $"{e.Exception}{Environment.NewLine}";
                }

                Trace.Text += text;
            });
        }

        private async void Connect(object sender, RoutedEventArgs e)
        {
            var options = new MqttClientOptions
            {
                Server = Server.Text,
                UserName = User.Text,
                Password = Password.Text,
                ClientId = ClientId.Text,
                TlsOptions = { UseTls = UseTls.IsChecked == true },
                ConnectionType = UseTcp.IsChecked == true ? MqttConnectionType.Tcp : MqttConnectionType.Ws
            };
            
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.DisconnectAsync();
                }

                var factory = new MqttClientFactory();
                _mqttClient = factory.CreateMqttClient(options);
                await _mqttClient.ConnectAsync();
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Publish(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

            try
            {
                var qos = MqttQualityOfServiceLevel.AtMostOnce;
                if (QoS1.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.AtLeastOnce;
                }

                if (QoS2.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.ExactlyOnce;
                }

                var payload = new byte[0];
                if (Text.IsChecked == true)
                {
                    payload = Encoding.UTF8.GetBytes(Payload.Text);
                }

                if (Base64.IsChecked == true)
                {
                    payload = Convert.FromBase64String(Payload.Text);
                }

                var message = new MqttApplicationMessage(
                    Topic.Text,
                    payload,
                    qos,
                    Retain.IsChecked == true);

                await _mqttClient.PublishAsync(message);
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Disconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            Trace.Text = string.Empty;
        }

        private async void Subscribe(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

            try
            {
                var qos = MqttQualityOfServiceLevel.AtMostOnce;
                if (SubscribeQoS1.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.AtLeastOnce;
                }

                if (SubscribeQoS2.IsChecked == true)
                {
                    qos = MqttQualityOfServiceLevel.ExactlyOnce;
                }

                await _mqttClient.SubscribeAsync(new TopicFilter(SubscribeTopic.Text, qos));
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }

        private async void Unsubscribe(object sender, RoutedEventArgs e)
        {
            if (_mqttClient == null)
            {
                return;
            }

            try
            {
                await _mqttClient.UnsubscribeAsync(SubscribeTopic.Text);
            }
            catch (Exception exception)
            {
                Trace.Text += exception + Environment.NewLine;
            }
        }
    }
}
